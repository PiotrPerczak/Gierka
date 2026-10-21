using UnityEngine;
using UnityEngine.Serialization;

public class PlayerStats : MonoBehaviour
{
    [Header("Poziom i doświadczenie")]
    public int currentLevel = 1;
    public float currentExp = 0f;
    public float expToNextLevel = 100f;
    public int unspentSkillPoints = 0;

    [Header("Obrażenia")]
    [FormerlySerializedAs("magicDamageMultiplier")]
    [Min(0f)] public float damageMultiplier = 1f;

    [Range(0f, 1f)] public float critChance = 0f;
    [Min(1f)] public float critDamageMultiplier = 2f;

    [Header("Szybkość ataku")]
    [Min(0.01f)] public float attackSpeedMultiplier = 1f;

    [FormerlySerializedAs("lightningAttackSpeedLevel")]
    [Min(0)] public int attackSpeedLevel = 0;

    [Min(0f)] public float attackSpeedPerLevel = 0.15f;

    [Header("Pociski")]
    [Min(0)] public int extraProjectileCount = 0;

    [FormerlySerializedAs("lightningChainCount")]
    [Min(0)] public int projectileBounces = 0;

    [Min(0.01f)] public float projectileSpeedMultiplier = 1f;

    [Header("Piorun - niezależne przeskoki, nie odbicia pocisków")]
    [Min(0)] public int lightningChains = 0;

    [Header("Obszar i odpychanie")]
    [Min(0.01f)] public float sizeMultiplier = 1f;
    [Min(0f)] public float knockbackMultiplier = 1f;

    [Header("Ruch")]
    [Min(0.01f)] public float movementSpeedMultiplier = 1f;

    [Header("Zdrowie")]
    [Min(1f)] public float maxHealth = 100f;
    public float currentHealth = 100f;

    [Tooltip("Liczba punktów HP odzyskiwanych na sekundę.")]
    [Min(0f)] public float healthRegen = 0f;

    [Tooltip("0.05 oznacza leczenie za 5% zadanych obrażeń.")]
    [Range(0f, 1f)] public float lifesteal = 0f;

    [Header("Mana")]
    [Min(0f)] public float maxMana = 100f;
    public float currentMana = 100f;

    [Tooltip("Liczba punktów many odzyskiwanych na sekundę.")]
    [Min(0f)] public float manaRegen = 5f;

    [Header("Statystyki rozgrywki")]
    public int kills = 0;
    public int coins = 0;

    // Zachowane dla obecnego panelu ulepszeń.
    public int magicLevel = 1;

    public float AttackSpeedMultiplier =>
        Mathf.Max(
            0.01f,
            attackSpeedMultiplier *
            (1f + Mathf.Max(0, attackSpeedLevel) *
            Mathf.Max(0f, attackSpeedPerLevel))
        );

    public bool IsAlive => currentHealth > 0f;

    void Awake()
    {
        // Na początku gry gracz ma pełne HP i manę.
        currentHealth = maxHealth;
        currentMana = maxMana;
    }

    void Update()
    {
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        currentMana = Mathf.Clamp(currentMana, 0f, maxMana);

        if (!IsAlive || Time.timeScale <= 0f)
            return;

        Heal(Mathf.Max(0f, healthRegen) * Time.deltaTime);
        RestoreMana(Mathf.Max(0f, manaRegen) * Time.deltaTime);
    }

    public void Heal(float amount)
    {
        if (!IsAlive || !ValidPositiveAmount(amount))
            return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }

    public void RestoreMana(float amount)
    {
        if (!ValidPositiveAmount(amount))
            return;

        currentMana = Mathf.Min(maxMana, currentMana + amount);
    }

    public bool TrySpendMana(float amount)
    {
        if (!IsAlive || float.IsNaN(amount) ||
            float.IsInfinity(amount) || amount < 0f)
            return false;

        if (currentMana < amount)
            return false;

        currentMana -= amount;
        return true;
    }

    public void TakeDamage(float amount)
    {
        if (!IsAlive || !ValidPositiveAmount(amount))
            return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);
    }

    static bool ValidPositiveAmount(float amount)
    {
        return amount > 0f &&
            !float.IsNaN(amount) &&
            !float.IsInfinity(amount);
    }

    // One shared hit pipeline: no duplicate damage multiplier or lifesteal on overkill.
    public float DealSpellDamage(EnemyController target, float baseDamage, Vector3 hitPoint,
        Vector3 incomingDirection, bool damageAlreadyScaled = false, float impactMultiplier = 1f)
    {
        if (!IsAlive || !target || target.currentHealth <= 0f || !ValidPositiveAmount(baseDamage))
            return 0f;
        float damage = baseDamage * (damageAlreadyScaled ? 1f : Mathf.Max(0f, damageMultiplier));
        if (critChance >= 1f || (critChance > 0f && Random.value < Mathf.Clamp01(critChance)))
            damage *= Mathf.Max(1f, critDamageMultiplier);
        if (!ValidPositiveAmount(damage)) return 0f;
        float before = target.currentHealth;
        target.TakeDamage(damage, hitPoint, incomingDirection,
            damage * Mathf.Max(0f, knockbackMultiplier) * Mathf.Max(0f, impactMultiplier));
        float applied = Mathf.Clamp(before - target.currentHealth, 0f, before);
        Heal(applied * Mathf.Clamp01(lifesteal));
        return applied;
    }
    public void AddExp(float amount)
    {
        if (ValidPositiveAmount(amount))
            currentExp += amount;
    }

    public bool ConfirmLevelUp()
    {
        if (expToNextLevel <= 0f || currentExp < expToNextLevel)
            return false;

        currentExp -= expToNextLevel;
        currentLevel++;
        unspentSkillPoints++;
        expToNextLevel *= 1.5f;
        return true;
    }

    public bool UpgradeDamage()
    {
        if (unspentSkillPoints <= 0)
            return false;

        unspentSkillPoints--;
        magicLevel++;
        damageMultiplier += 1f;
        return true;
    }

    public bool UpgradeAttackSpeed()
    {
        if (unspentSkillPoints <= 0)
            return false;

        unspentSkillPoints--;
        attackSpeedLevel++;
        return true;
    }

    public bool UpgradeProjectileBounces()
    {
        if (unspentSkillPoints <= 0)
            return false;

        unspentSkillPoints--;
        projectileBounces++;
        return true;
    }

    // Zgodność ze starymi spellami i panelem.
    // Później przełączymy je na ogólne nazwy.

    public float magicDamageMultiplier
    {
        get => damageMultiplier;
        set => damageMultiplier = Mathf.Max(0f, value);
    }

    public int lightningAttackSpeedLevel
    {
        get => attackSpeedLevel;
        set => attackSpeedLevel = Mathf.Max(0, value);
    }

    public int lightningChainCount
    {
        get => lightningChains;
        set => lightningChains = Mathf.Max(0, value);
    }

    public float LightningAttackSpeedMultiplier =>
        AttackSpeedMultiplier;

    public bool UpgradeMagicDamage() => UpgradeDamage();

    public bool UpgradeLightningAttackSpeed() =>
        UpgradeAttackSpeed();

    public bool UpgradeLightningChains()
    {
        if (unspentSkillPoints <= 0) return false;
        unspentSkillPoints--; lightningChains++; return true;
    }
}
