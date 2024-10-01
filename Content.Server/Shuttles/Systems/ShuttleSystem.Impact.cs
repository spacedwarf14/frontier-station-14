using System.Diagnostics;
using System.Numerics;
using System.Threading;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Shuttles.Components;
using Content.Shared.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
//using Content.Server.Shuttles.Systems;

namespace Content.Server.Shuttles.Systems;

public sealed partial class ShuttleSystem
{
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;

    /// <summary>
    /// Minimum velocity difference between 2 bodies for a shuttle "impact" to occur.
    /// </summary>
    private const int MinimumImpactVelocity = 10;

    private readonly SoundCollectionSpecifier _shuttleImpactSound = new("ShuttleImpactSound");

    private void InitializeImpact(SharedTransformSystem _transform)
    {
        SubscribeLocalEvent<ShuttleComponent, StartCollideEvent>(OnShuttleCollide);
    }

    private void OnShuttleCollide(EntityUid uid, ShuttleComponent component, ref StartCollideEvent args)
    {
        if (!HasComp<ShuttleComponent>(args.OtherEntity))
            return;

        var ourBody = args.OurBody;
        var otherBody = args.OtherBody;

        // TODO: Would also be nice to have a continuous sound for scraping.
        var ourXform = Transform(uid);

        if (ourXform.MapUid == null)
            return;

        var otherXform = Transform(args.OtherEntity);

        var ourPoint = Vector2.Transform(args.WorldPoint, ourXform.InvWorldMatrix);
        var otherPoint = Vector2.Transform(args.WorldPoint, otherXform.InvWorldMatrix);

        var ourVelocity = _physics.GetLinearVelocity(uid, ourPoint, ourBody, ourXform);
        var otherVelocity = _physics.GetLinearVelocity(args.OtherEntity, otherPoint, otherBody, otherXform);
        var jungleDiff = (ourVelocity - otherVelocity).Length();

        if (jungleDiff < MinimumImpactVelocity)
        {
            return;
        }

        Log.Debug($"{ourBody} {args.OurEntity} has hit {otherBody} {args.OtherEntity} at {jungleDiff} m/s!");
        Log.Debug($"coords of hit: {ourPoint.X} {ourPoint.Y}, {ourXform.Coordinates.X} {ourXform.Coordinates.Y} ");




        var coordinates = new EntityCoordinates(ourXform.MapUid.Value, args.WorldPoint);
        var volume = MathF.Min(10f, 1f * MathF.Pow(jungleDiff, 0.5f) - 5f);
        var audioParams = AudioParams.Default.WithVariation(SharedContentAudioSystem.DefaultVariation).WithVolume(volume);

        _audio.PlayPvs(_shuttleImpactSound, coordinates, audioParams);

        Log.Debug($"GRAND SLAM!");

        // BOOOM
        /*var gridUid = args.OurEntity;
        if (!TryComp(gridUid, out MapGridComponent? grid))
            return;

        var coords = grid.CoordinatesToTile(coordinates);
        _explosionSystem.QueueExplosion(coordinates.ToMap(EntityManager, _transform),
            ExplosionSystem.DefaultExplosionPrototypeId,
            100 * jungleDiff,
            4,
            50 * jungleDiff,
            args.OurEntity,
            maxTileBreak: 4);*/

/*
        Timer.Spawn(_gameTiming.TickPeriod,
            () => _explosionSystem.QueueExplosion(coords, ExplosionSystem.DefaultExplosionPrototypeId,
                4, 1, 2, args.Target, maxTileBreak: 0), // it gibs, damage doesn't need to be high.
            CancellationToken.None);*/



    }
}
