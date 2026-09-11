namespace NuevoMMO.Core;

public static class WorldGrid
{
    public const int LogicalTileSize = 32;
}

public readonly record struct CollisionBounds
{
    public CollisionBounds(float left, float top, float right, float bottom)
    {
        if (!float.IsFinite(left) || !float.IsFinite(top) || !float.IsFinite(right) || !float.IsFinite(bottom) || left > right || top > bottom)
            throw new ArgumentException("CollisionBounds inválido.");
        Left = left; Top = top; Right = right; Bottom = bottom;
    }
    public float Left { get; }
    public float Top { get; }
    public float Right { get; }
    public float Bottom { get; }
    public float Width => Right - Left;
    public float Height => Bottom - Top;
    public Vector2Data Center => new((Left + Right) * .5f, (Top + Bottom) * .5f);
    public bool Intersects(CollisionBounds other) => Left < other.Right && Right > other.Left && Top < other.Bottom && Bottom > other.Top;
    public bool IsInside(BoundsData world) => world.IsValid && Left >= world.Minimum.X && Top >= world.Minimum.Y && Right <= world.Maximum.X && Bottom <= world.Maximum.Y;
    public static CollisionBounds Union(CollisionBounds a, CollisionBounds b) => new(MathF.Min(a.Left,b.Left),MathF.Min(a.Top,b.Top),MathF.Max(a.Right,b.Right),MathF.Max(a.Bottom,b.Bottom));
}

public abstract record CollisionShape
{
    protected CollisionShape(Vector2Data offset) { if (!offset.IsFinite) throw new ArgumentException("Offset no finito.", nameof(offset)); Offset=offset; }
    public Vector2Data Offset { get; }
    public Vector2Data CenterAt(Vector2Data origin)=>origin+Offset;
    public abstract CollisionBounds BoundsAt(Vector2Data origin);
}

public sealed record CircleCollisionShape : CollisionShape
{
    public CircleCollisionShape(float radius, Vector2Data offset=default):base(offset){if(!float.IsFinite(radius)||radius<=0)throw new ArgumentOutOfRangeException(nameof(radius));Radius=radius;}
    public float Radius{get;}
    public override CollisionBounds BoundsAt(Vector2Data origin){var c=CenterAt(origin);return new(c.X-Radius,c.Y-Radius,c.X+Radius,c.Y+Radius);}
}

public sealed record BoxCollisionShape : CollisionShape
{
    public BoxCollisionShape(float halfWidth,float halfHeight,Vector2Data offset=default):base(offset){if(!float.IsFinite(halfWidth)||halfWidth<=0)throw new ArgumentOutOfRangeException(nameof(halfWidth));if(!float.IsFinite(halfHeight)||halfHeight<=0)throw new ArgumentOutOfRangeException(nameof(halfHeight));HalfWidth=halfWidth;HalfHeight=halfHeight;}
    public float HalfWidth{get;} public float HalfHeight{get;}
    public override CollisionBounds BoundsAt(Vector2Data origin){var c=CenterAt(origin);return new(c.X-HalfWidth,c.Y-HalfHeight,c.X+HalfWidth,c.Y+HalfHeight);}
}

public sealed record CapsuleCollisionShape : CollisionShape
{
    public CapsuleCollisionShape(Vector2Data a,Vector2Data b,float radius,Vector2Data offset=default):base(offset){if(!a.IsFinite||!b.IsFinite||a==b)throw new ArgumentException("Segmento de cápsula inválido.");if(!float.IsFinite(radius)||radius<=0)throw new ArgumentOutOfRangeException(nameof(radius));A=a;B=b;Radius=radius;}
    public Vector2Data A{get;} public Vector2Data B{get;} public float Radius{get;}
    public Vector2Data WorldA(Vector2Data origin)=>origin+Offset+A; public Vector2Data WorldB(Vector2Data origin)=>origin+Offset+B;
    public override CollisionBounds BoundsAt(Vector2Data origin){var a=WorldA(origin);var b=WorldB(origin);return new(MathF.Min(a.X,b.X)-Radius,MathF.Min(a.Y,b.Y)-Radius,MathF.Max(a.X,b.X)+Radius,MathF.Max(a.Y,b.Y)+Radius);}
}

public sealed record ConvexPolygonCollisionShape : CollisionShape
{
    public ConvexPolygonCollisionShape(IEnumerable<Vector2Data> points,Vector2Data offset=default):base(offset){ArgumentNullException.ThrowIfNull(points);Points=points.ToArray();if(Points.Length<3||Points.Any(static p=>!p.IsFinite)||!CollisionShapeValidation.IsStrictlyConvex(Points))throw new ArgumentException("Polígono convexo inválido.",nameof(points));}
    public Vector2Data[] Points{get;}
    public Vector2Data[] WorldPoints(Vector2Data origin){var t=origin+Offset;return Points.Select(p=>p+t).ToArray();}
    public override CollisionBounds BoundsAt(Vector2Data origin){var p=WorldPoints(origin);var l=p[0].X;var r=l;var t=p[0].Y;var b=t;for(var i=1;i<p.Length;i++){l=MathF.Min(l,p[i].X);r=MathF.Max(r,p[i].X);t=MathF.Min(t,p[i].Y);b=MathF.Max(b,p[i].Y);}return new(l,t,r,b);}
}

public sealed record CompoundCollisionShape : CollisionShape
{
    public CompoundCollisionShape(IEnumerable<CollisionShape> parts,Vector2Data offset=default):base(offset){ArgumentNullException.ThrowIfNull(parts);Parts=parts.ToArray();if(Parts.Length==0)throw new ArgumentException("Collider compuesto vacío.",nameof(parts));}
    public CollisionShape[] Parts{get;}
    public override CollisionBounds BoundsAt(Vector2Data origin){var o=origin+Offset;var result=Parts[0].BoundsAt(o);for(var i=1;i<Parts.Length;i++)result=CollisionBounds.Union(result,Parts[i].BoundsAt(o));return result;}
}

public sealed record EntityCollisionProfile
{
    public EntityCollisionProfile(CollisionShape? movementCollider=null,IEnumerable<CollisionShape>? hurtboxes=null,CollisionShape? interactionShape=null,IEnumerable<CollisionShape>? hitboxes=null,float navigationRadius=0,bool blocksMovement=false){if(!float.IsFinite(navigationRadius)||navigationRadius<0)throw new ArgumentOutOfRangeException(nameof(navigationRadius));MovementCollider=movementCollider;Hurtboxes=hurtboxes?.ToArray()??[];InteractionShape=interactionShape;Hitboxes=hitboxes?.ToArray()??[];NavigationRadius=navigationRadius;BlocksMovement=blocksMovement;}
    public CollisionShape? MovementCollider{get;} public CollisionShape[] Hurtboxes{get;} public CollisionShape? InteractionShape{get;} public CollisionShape[] Hitboxes{get;} public float NavigationRadius{get;} public bool BlocksMovement{get;}
    public static EntityCollisionProfile Empty{get;}=new();
}

internal static class CollisionShapeValidation
{
    public static bool IsStrictlyConvex(IReadOnlyList<Vector2Data> p){float sign=0;for(var i=0;i<p.Count;i++){var a=p[i];var b=p[(i+1)%p.Count];var c=p[(i+2)%p.Count];var cross=Cross(b-a,c-b);if(MathF.Abs(cross)<=1e-5f)return false;var current=MathF.Sign(cross);if(sign==0)sign=current;else if(current!=sign)return false;}return sign!=0;}
    private static float Cross(Vector2Data a,Vector2Data b)=>a.X*b.Y-a.Y*b.X;
}
