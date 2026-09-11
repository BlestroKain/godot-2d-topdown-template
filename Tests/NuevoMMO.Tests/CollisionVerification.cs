using System.Runtime.CompilerServices;
using NuevoMMO.Core;
using NuevoMMO.Server.Entities;
using NuevoMMO.Server.Systems;

internal static class CollisionVerification
{
    [ModuleInitializer]
    internal static void Verify()
    {
        Check(WorldGrid.LogicalTileSize == 32, "Collision: tile lógico 32 sin imponer grid movement");

        var circle = new CircleCollisionShape(5);
        var box = new BoxCollisionShape(4, 3);
        Check(CollisionGeometry.Overlaps(circle, new(0, 0), box, new(6, 0)), "Collision: círculo/caja solapan");
        Check(!CollisionGeometry.Overlaps(circle, new(0, 0), box, new(9, 0)), "Collision: tangencia no es penetración");

        var capsule = new CapsuleCollisionShape(new(-5, 0), new(5, 0), 2);
        Check(CollisionGeometry.Overlaps(capsule, default, new CircleCollisionShape(2), new(0, 3)), "Collision: cápsula/círculo");

        var compound = new CompoundCollisionShape([new CircleCollisionShape(2, new(-4,0)), new CircleCollisionShape(2, new(4,0))]);
        Check(CollisionGeometry.Overlaps(compound, default, new CircleCollisionShape(2), new(5,0)), "Collision: collider compuesto");

        var bounds = new BoundsData(new(0,0), new(20,20));
        var motion = MotionSolver2D.Resolve(new(10,10), new(20,5), bounds, new BoxCollisionShape(2,2));
        Check(motion.Final.X <= 18.001f && motion.Final.Y > 10, "Collision: movimiento continuo desliza contra límite");

        var probeBlocked = false;
        var tunneled = MotionSolver2D.Resolve(new(2,10), new(16,0), bounds, new CircleCollisionShape(1), p =>
        {
            if (p.X >= 9 && p.X <= 11) { probeBlocked = true; return true; }
            return false;
        });
        Check(probeBlocked && tunneled.Final.X < 9, "Collision: substeps de 1 unidad evitan tunneling técnico");

        var source = PlayerAt(1, new(5,5), "Source");
        var target = PlayerAt(2, new(12,5), "Target");
        source.ConfigureCollision(new EntityCollisionProfile(
            movementCollider: new CircleCollisionShape(2),
            interactionShape: new CircleCollisionShape(10),
            hitboxes: [new CircleCollisionShape(8)], navigationRadius: 3));
        target.ConfigureCollision(new EntityCollisionProfile(
            movementCollider: new CircleCollisionShape(2),
            hurtboxes: [new CircleCollisionShape(2)], blocksMovement: true));
        Check(SemanticCollisionService.IsWithinInteractionReach(source, target, 1), "Collision: InteractionShape independiente del fallback");
        Check(SemanticCollisionService.AnyHitboxTouchesAnyHurtbox(source, target), "Collision: Hitbox consulta Hurtbox");
        Check(SemanticCollisionService.NavigationClearance(source) == 3, "Collision: NavigationRadius separado");

        VerifyMapCollisionRuntime();
    }

    private static void VerifyMapCollisionRuntime()
    {
        var wall = new MapCollisionDefinition(
            Guid.NewGuid(),
            new MapShapeDefinition(MapShapeKind.Rectangle, new(10, 20), new(4, 20)),
            blocksMovement: true,
            blocksProjectiles: true,
            blocksVision: false,
            navigationObstacle: true);
        var concave = new MapCollisionDefinition(
            Guid.NewGuid(),
            new MapShapeDefinition(MapShapeKind.Polygon, new(25, 25), points:
            [new(-3,-3), new(5,-3), new(5,5), new(1,1), new(-3,5)]));
        var map = new MapDefinition(
            DefinitionId.New(), new("maps.collision.verify"), "Collision Verify", "", true, 1, null,
            new("maps.collision.verify.visual"), new(new(0,0), new(40,40)), new(4,4), new(32,32),
            new MapContentDefinition(collisions: [wall, concave]));

        var compiledConcave = MapCollisionShapeCompiler.Compile(concave.Shape);
        Check(compiledConcave is CompoundCollisionShape, "Collision: polígono cóncavo se compila a collider compuesto");
        var concaveBounds = compiledConcave.BoundsAt(default);
        Check(MathF.Abs(concaveBounds.Left - 22) < .001f && MathF.Abs(concaveBounds.Top - 22) < .001f &&
              MathF.Abs(concaveBounds.Right - 30) < .001f && MathF.Abs(concaveBounds.Bottom - 30) < .001f,
            "Collision: puntos de polígono son locales respecto a Center");

        var runtime = MapCollisionRuntime.For(map);
        var mover = new CircleCollisionShape(1);
        Check(runtime.BlocksMovement(mover, new(7.5f,20)), "Collision: MapCollisionDefinition bloquea volumen de movimiento");
        Check(!runtime.BlocksMovement(mover, new(6,20)), "Collision: broadphase no crea falsos positivos");
        Check(runtime.BlocksMovement(null, new(24,24)), "Collision: runtime respeta offset Center de polígono");
        Check(runtime.BlocksProjectile(null, new(10,20)), "Collision: BlocksProjectiles usa la misma geometría de mapa");
        Check(!runtime.BlocksVision(null, new(10,20)), "Collision: flags semánticos de mapa permanecen separados");
        Check(runtime.IsNavigationObstacle(null, new(10,20)), "Collision: NavigationObstacle disponible para pathfinding");

        var projectile = new Projectile(
            new EntityId(99), null, new EntityId(1), null, new MapInstanceId(1),
            new Vector2Data(2,20), Vector2Data.Right, 200, 100, 1000, 1,
            new ContentKey("template.player"), "Projectile verify");
        var projectileStep = new ProjectileSystem().Step(projectile, map, 50);
        Check(projectileStep.Expired && projectile.Position.X < 9,
            "Collision: proyectil no atraviesa BlocksProjectiles a alta velocidad");
    }

    private static Player PlayerAt(long id, Vector2Data position, string name)
        => new(new EntityId(id), new AccountId(Guid.NewGuid()), new CharacterId(Guid.NewGuid()), new MapInstanceId(1), position, new ContentKey("template.player"), name);

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException("FAIL: " + name);
        Console.WriteLine("PASS: " + name);
    }
}
