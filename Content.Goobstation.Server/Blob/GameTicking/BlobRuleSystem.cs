// SPDX-FileCopyrightText: 2024 Aiden <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2024 Fishbait <Fishbait@git.ml>
// SPDX-FileCopyrightText: 2024 fishbait <gnesse@gmail.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <93730715+Aviu00@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Goobstation.Common.Blob;
using Content.Goobstation.Server.Blob.Components;
using Content.Goobstation.Shared.Blob.Components;
using Content.Server.AlertLevel;
using Content.Server.Antag;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.Mind;
using Content.Server.Nuke;
using Content.Server.Objectives;
using Content.Server.RoundEnd;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.GameTicking.Components;
using Content.Shared.Objectives.Components;
using Robust.Server.Player;
using Robust.Shared.Player;

namespace Content.Goobstation.Server.Blob.GameTicking;

public sealed class BlobRuleSystem : GameRuleSystem<BlobRuleComponent>
{
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly NukeCodePaperSystem _nukeCode = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly ObjectivesSystem _objectivesSystem = default!;
    [Dependency] private readonly AlertLevelSystem _alertLevelSystem = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlobRuleComponent, AfterAntagEntitySelectedEvent>(AfterAntagSelected);
    }

    protected override void Started(EntityUid uid, BlobRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        var activeRules = QueryActiveRules();
        while (activeRules.MoveNext(out var entityUid, out _, out _, out _))
        {
            if(uid == entityUid)
                continue;

            GameTicker.EndGameRule(uid, gameRule);
            Log.Warning("blob is active!!! remove!");
            break;
        }
    }

    protected override void ActiveTick(EntityUid uid, BlobRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        component.Accumulator += frameTime;

        if(component.Accumulator < 10)
            return;

        component.Accumulator = 0;

        var check = new Dictionary<EntityUid, long>();
        var blobCoreQuery = EntityQueryEnumerator<BlobCoreComponent, MetaDataComponent, TransformComponent>();
        while (blobCoreQuery.MoveNext(out var ent, out var comp, out var md, out var xform))
        {
            if (TerminatingOrDeleted(ent, md) ||
                !CheckBlobInStation(ent, xform, out var stationUid))
            {
                continue;
            }

            check.TryAdd(stationUid.Value, 0);

            check[stationUid.Value] += comp.BlobTiles.Count;
        }

        foreach (var (station, length) in check.AsParallel())
        {
            CheckChangeStage(station, component, length);
        }
    }

    private bool CheckBlobInStation(EntityUid blobCore, TransformComponent? xform, [NotNullWhen(true)] out EntityUid? stationUid)
    {
        var station = _stationSystem.GetOwningStation(blobCore, xform);
        if (station == null || !HasComp<StationEventEligibleComponent>(station.Value))
        {
            _chatManager.SendAdminAlert(blobCore, Loc.GetString("blob-alert-out-off-station"));
            QueueDel(blobCore);
            stationUid = null;
            return false;
        }

        stationUid = station.Value;
        return true;
    }

    private const string StationAlertCritical = "delta";
    private const string StationAlertDetected = "red";

    private void CheckChangeStage(
        Entity<StationBlobConfigComponent?> stationUid,
        BlobRuleComponent blobRuleComp,
        long blobTilesCount)
    {
        Resolve(stationUid, ref stationUid.Comp, false);

        var stationName = Name(stationUid);

        if (blobTilesCount >= (stationUid.Comp?.StageBegin ?? StationBlobConfigComponent.DefaultStageBegin)
            && _roundEndSystem.ExpectedCountdownEnd != null)
        {
            _roundEndSystem.CancelRoundEndCountdown(checkCooldown: false);
            _chatSystem.DispatchStationAnnouncement(stationUid,
                Loc.GetString("blob-alert-recall-shuttle"),
                Loc.GetString("Station"),
                false,
                null,
                Color.Red);
        }

        switch (blobRuleComp.Stage)
        {
            case BlobStage.Default when blobTilesCount >= (stationUid.Comp?.StageBegin ?? StationBlobConfigComponent.DefaultStageBegin):
                blobRuleComp.Stage = BlobStage.Begin;

                _chatSystem.DispatchGlobalAnnouncement(
                    Loc.GetString("blob-alert-detect"),
                    stationName,
                    true,
                    blobRuleComp.DetectedAudio,
                    Color.Red);

                _alertLevelSystem.SetLevel(stationUid, StationAlertDetected, true, true, true, true);

                RaiseLocalEvent(stationUid,
                    new BlobChangeLevelEvent
                {
                    Station = stationUid,
                    Level = blobRuleComp.Stage
                },
                    broadcast: true);
                return;
            case BlobStage.Begin when blobTilesCount >= (stationUid.Comp?.StageCritical ?? StationBlobConfigComponent.DefaultStageCritical):
            {
                if (_nukeCode.SendNukeCodes(stationUid))//send the nuke code?
                {
                    blobRuleComp.Stage = BlobStage.Critical;
                    _chatSystem.DispatchGlobalAnnouncement(
                    Loc.GetString("blob-alert-critical"),
                    stationName,
                    true,
                    blobRuleComp.CriticalAudio,
                    Color.Red);
                }
                else
                {
                    blobRuleComp.Stage = BlobStage.Critical;
                    _chatSystem.DispatchGlobalAnnouncement(
                    Loc.GetString("blob-alert-critical-NoNukeCode"),
                    stationName,
                    true,
                    blobRuleComp.CriticalAudio,
                    Color.Red);
                }

                _alertLevelSystem.SetLevel(stationUid, StationAlertCritical, true, true, true, true);

                RaiseLocalEvent(stationUid,
                    new BlobChangeLevelEvent
                {
                    Station = stationUid,
                    Level = blobRuleComp.Stage
                },
                    broadcast: true);
                return;
            }
            case BlobStage.Critical when blobTilesCount >= (stationUid.Comp?.StageTheEnd ?? StationBlobConfigComponent.DefaultStageEnd):
            {
                blobRuleComp.Stage = BlobStage.TheEnd;
                _roundEndSystem.EndRound();

                RaiseLocalEvent(stationUid,
                    new BlobChangeLevelEvent
                {
                    Station = stationUid,
                    Level = blobRuleComp.Stage
                },
                    broadcast: true);
                return;
            }
        }
    }

    protected override void AppendRoundEndText(
        EntityUid uid,
        BlobRuleComponent blob,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent ev)
    {
        if (blob.Blobs.Count < 1)
            return;

        var result = Loc.GetString("blob-round-end-result", ("blobCount", blob.Blobs.Count));

        // yeah this is duplicated from traitor rules lol, there needs to be a generic rewrite where it just goes through all minds with objectives
        foreach (var (mindId, mind) in blob.Blobs)
        {
            var name = mind.CharacterName;
            _player.TryGetSessionByEntity(mindId, out var session);
            var username = session?.Name;

            var objectives = mind.Objectives.ToArray();
            if (objectives.Length == 0)
            {
                if (username != null)
                {
                    if (name == null)
                        result += "\n" + Loc.GetString("blob-user-was-a-blob", ("user", username));
                    else
                    {
                        result += "\n" + Loc.GetString("blob-user-was-a-blob-named",
                            ("user", username),
                            ("name", name));
                    }
                }
                else if (name != null)
                    result += "\n" + Loc.GetString("blob-was-a-blob-named", ("name", name));

                continue;
            }

            if (username != null)
            {
                if (name == null)
                {
                    result += "\n" + Loc.GetString("blob-user-was-a-blob-with-objectives",
                        ("user", username));
                }
                else
                {
                    result += "\n" + Loc.GetString("blob-user-was-a-blob-with-objectives-named",
                        ("user", username),
                        ("name", name));
                }
            }
            else if (name != null)
                result += "\n" + Loc.GetString("blob-was-a-blob-with-objectives-named", ("name", name));

            foreach (var objectiveGroup in objectives.GroupBy(o => Comp<ObjectiveComponent>(o).LocIssuer))
            {
                foreach (var objective in objectiveGroup)
                {

                    var info = _objectivesSystem.GetInfo(objective, mindId, mind);
                    if (info == null)
                        continue;

                    var objectiveTitle = info.Value.Title;
                    var progress = info.Value.Progress;

                    if (progress > 0.99f)
                    {
                        result += "\n- " + Loc.GetString(
                            "objective-condition-success",
                            ("condition", objectiveTitle),
                            ("markupColor", "green")
                        );
                    }
                    else
                    {
                        result += "\n- " + Loc.GetString(
                            "objective-condition-fail",
                            ("condition", objectiveTitle),
                            ("progress", (int) (progress * 100)),
                            ("markupColor", "red")
                        );
                    }
                }
            }
        }

        ev.AddLine(result);
    }

    public void MakeBlob(EntityUid player)
    {
        var comp = EnsureComp<BlobCarrierComponent>(player);
        comp.HasMind = HasComp<ActorComponent>(player);
        comp.TransformationDelay = 10 * 60; // 10min
    }

    private void AfterAntagSelected(EntityUid uid, BlobRuleComponent component, AfterAntagEntitySelectedEvent args)
    {
        MakeBlob(args.EntityUid);
    }
}
