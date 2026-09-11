namespace NuevoMMO.Core;

/// <summary>
/// Técnica canónica de ataque básico. No es contenido de juego: es el contrato mínimo
/// para que el runtime no dependa de TrainingDummyFixture.
/// </summary>
public static class CanonicalCombatContent
{
    public static readonly DefinitionId BasicAttackId =
        DefinitionId.From(Guid.Parse("2c1f0b3a-7e64-4c8d-9a11-6b4e2f8d1c70"));

    public static readonly ContentKey BasicAttackKey = new("techniques.basic_attack");

    public static TechniqueDefinition BasicAttack()
        => new(
            BasicAttackId,
            BasicAttackKey,
            "Ataque básico",
            "Golpe cuerpo a cuerpo del arma o desarmado.",
            true,
            1,
            ["canonical", "basic"],
            new ContentKey("visuals.technique.basic_attack"),
            Element.Neutral,
            targeting: new TechniqueTargetingDefinition(
                TechniqueTargetMode.Entity,
                TechniqueTargetRelation.Enemy,
                range: 64,
                requiresLineOfSight: false),
            timing: new TechniqueTimingDefinition(castMilliseconds: 0, cooldownMilliseconds: 600),
            actions:
            [
                new TechniqueActionDefinition(
                    TechniqueActionKind.Damage,
                    TechniqueActionMoment.Impact,
                    amount: 8,
                    element: Element.Neutral,
                    scaling: new Dictionary<StatId, float> { [StatId.Strength] = 0.4f })
            ]);
}
