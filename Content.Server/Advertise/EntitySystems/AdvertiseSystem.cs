// SPDX-FileCopyrightText: 2024 Aidenkrz <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2024 Tayrtahn <tayrtahn@gmail.com>
// SPDX-FileCopyrightText: 2024 Wrexbe (Josh) <81056464+wrexbe@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 wrexbe <wrexbe@protonmail.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 CerberusWolfie <wb.johnb.willis@gmail.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 John Willis <143434770+CerberusWolfie@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Mnemotechnican <69920617+Mnemotechnician@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Tim <timfalken@hotmail.com>
// SPDX-FileCopyrightText: 2025 Timfa <timfalken@hotmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Advertise.Components;
using Content.Server.Chat.Systems;
using Content.Server.Power.Components;
using Content.Shared.Chat; // Einstein Engines - Languages
using Content.Shared.VendingMachines;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Advertise.EntitySystems;

public sealed class AdvertiseSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    /// <summary>
    /// The maximum amount of time between checking if advertisements should be displayed
    /// </summary>
    private readonly TimeSpan _maximumNextCheckDuration = TimeSpan.FromSeconds(15);

    /// <summary>
    /// The next time the game will check if advertisements should be displayed
    /// </summary>
    private TimeSpan _nextCheckTime = TimeSpan.MinValue;

    public override void Initialize()
    {
        SubscribeLocalEvent<AdvertiseComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<ApcPowerReceiverComponent, AttemptAdvertiseEvent>(OnPowerReceiverAttemptAdvertiseEvent);
        SubscribeLocalEvent<VendingMachineComponent, AttemptAdvertiseEvent>(OnVendingAttemptAdvertiseEvent);

        _nextCheckTime = TimeSpan.MinValue;
    }

    private void OnMapInit(EntityUid uid, AdvertiseComponent advert, MapInitEvent args)
    {
        var prewarm = advert.Prewarm;
        RandomizeNextAdvertTime(advert, prewarm);
        _nextCheckTime = MathHelper.Min(advert.NextAdvertisementTime, _nextCheckTime);
    }

    private void RandomizeNextAdvertTime(AdvertiseComponent advert, bool prewarm = false)
    {
        var minDuration = prewarm ? 0 : Math.Max(1, advert.MinimumWait);
        var maxDuration = Math.Max(minDuration, advert.MaximumWait);
        var waitDuration = TimeSpan.FromSeconds(_random.Next(minDuration, maxDuration));

        advert.NextAdvertisementTime = _gameTiming.CurTime + waitDuration;
    }

    public void SayAdvertisement(EntityUid uid, AdvertiseComponent? advert = null)
    {
        if (!Resolve(uid, ref advert))
            return;

        var attemptEvent = new AttemptAdvertiseEvent(uid);
        RaiseLocalEvent(uid, ref attemptEvent);
        if (attemptEvent.Cancelled)
            return;

        bool hideChat = advert.HideFromChat; // Europa

        if (_prototypeManager.TryIndex(advert.Pack, out var advertisements))
            _chat.TrySendInGameICMessage(uid, Loc.GetString(_random.Pick(advertisements.Values)), InGameICChatType.Speak, hideChat: hideChat); // Europa-Edit | hideChat bool
    }

    public override void Update(float frameTime)
    {
        var currentGameTime = _gameTiming.CurTime;
        if (_nextCheckTime > currentGameTime)
            return;

        // _nextCheckTime starts at TimeSpan.MinValue, so this has to SET the value, not just increment it.
        _nextCheckTime = currentGameTime + _maximumNextCheckDuration;

        var query = EntityQueryEnumerator<AdvertiseComponent>();
        while (query.MoveNext(out var uid, out var advert))
        {
            if (currentGameTime > advert.NextAdvertisementTime)
            {
                SayAdvertisement(uid, advert);
                // The timer is always refreshed when it expires, to prevent mass advertising (ex: all the vending machines have no power, and get it back at the same time).
                RandomizeNextAdvertTime(advert);
            }
            _nextCheckTime = MathHelper.Min(advert.NextAdvertisementTime, _nextCheckTime);
        }
    }


    private static void OnPowerReceiverAttemptAdvertiseEvent(EntityUid uid, ApcPowerReceiverComponent powerReceiver, ref AttemptAdvertiseEvent args)
    {
        args.Cancelled |= !powerReceiver.Powered;
    }

    private static void OnVendingAttemptAdvertiseEvent(EntityUid uid, VendingMachineComponent machine, ref AttemptAdvertiseEvent args)
    {
        args.Cancelled |= machine.Broken;
    }
}

[ByRefEvent]
public record struct AttemptAdvertiseEvent(EntityUid? Advertiser)
{
    public bool Cancelled = false;
}
