using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CharacterPanelUI : MonoBehaviour
{
    [Header("Główny panel")]
    public GameObject panel; // Okno, które będziemy chować i pokazywać

    [Header("Referencje do tekstów")]
    public Text levelText;
    public Text expText;
    public Text pointsText;
    public Text magicDamageText;

    [Header("Piorun - nowe napisy TMP")]
    public TMPro.TMP_Text attackSpeedText;
    public TMPro.TMP_Text chainsText;
    public UnityEngine.UI.Button attackSpeedButton;
    public UnityEngine.UI.Button chainsButton;
    public StormSpell stormSpell;

    [Header("Wszystkie statystyki")]
    public TMPro.TMP_Text allStatsText;
    public TMPro.TMP_Text lightningChainsText;
    public UnityEngine.UI.Button lightningChainsButton;
    CursorLockMode previousCursorLock;
    bool previousCursorVisible;

    [Header("Referencja do gracza")]
    public PlayerStats playerStats;

    [Header("Ekwipunek")]
    public PlayerInventory playerInventory;
    public Image stormTomeIcon;

    void Start()
    {
        // Jeśli nie przeciągnąłeś gracza w inspektorze, znajdź go automatycznie
        if (playerStats == null)
        {
            playerStats = Object.FindAnyObjectByType<PlayerStats>();
        }

        if (playerInventory == null && playerStats != null)
            playerInventory = playerStats.GetComponent<PlayerInventory>();  
        if (stormSpell == null && playerStats != null)
            stormSpell = playerStats.GetComponent<StormSpell>();

        // Na starcie gry okno jest schowane
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    void Update()
    {
        // Sprawdzamy klawisz TAB (działa z New Input System)
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            TogglePanel();
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && panel != null && panel.activeSelf) TogglePanel();
        if (panel != null && panel.activeSelf) { FitPanel(); UpdateUI(); }
    }

    public void TogglePanel()
    {
        if (panel == null) return;

        bool isOpen = !panel.activeSelf;
        if (isOpen) { previousCursorLock = Cursor.lockState; previousCursorVisible = Cursor.visible; }
        panel.SetActive(isOpen);

        // Jeśli panel jest otwarty: odblokuj kursor myszy, żeby móc klikać
        // Jeśli zamknięty: ukryj/zablokuj wedle uznania
        Cursor.visible = isOpen || previousCursorVisible;
        Cursor.lockState = isOpen ? CursorLockMode.None : previousCursorLock;

        if (isOpen)
        {
            FitPanel();
            UpdateUI();
        }
    }

    void FitPanel()
    {
        if (!panel) return;
        var rect = panel.GetComponent<RectTransform>();
        var canvas = panel.GetComponentInParent<Canvas>();
        if (!canvas || !rect) return;
        var parentRect = canvas.rootCanvas.GetComponent<RectTransform>();
        float scale = Mathf.Clamp(Mathf.Min((parentRect.rect.width - 32f) / 1120f,
            (parentRect.rect.height - 32f) / 740f), .1f, 1f);
        rect.localScale = Vector3.one * scale;
    }
    public void UpdateUI()
    {
        if (playerStats == null)
            return;

        if (levelText != null)
            levelText.text = $"Poziom postaci: {playerStats.currentLevel}";

        if (expText != null)
        {
            bool ready =
                playerStats.currentExp >= playerStats.expToNextLevel;

            expText.text =
                $"EXP: {playerStats.currentExp:F0} / " +
                $"{playerStats.expToNextLevel:F0}" +
                (ready ? " — możesz awansować [+]" : "");
        }

        if (pointsText != null)
            pointsText.text =
                $"Punkty do rozdania: {playerStats.unspentSkillPoints}";

        if (magicDamageText != null)
            magicDamageText.text = $"Obrażenia: ×{playerStats.damageMultiplier:F2}";

        if (stormTomeIcon != null)
            stormTomeIcon.gameObject.SetActive(
                playerInventory != null && playerInventory.HasAirBook);

        float baseCooldown = stormSpell != null ? stormSpell.cooldown : 1f;
        float interval = Mathf.Max(0.08f, baseCooldown / playerStats.AttackSpeedMultiplier);
        if (attackSpeedText != null)
            attackSpeedText.text = $"Attack Speed: {playerStats.AttackSpeedMultiplier:F2}x | co {interval:F2} s";
        if (chainsText != null)
            chainsText.text = $"Odbicia pocisków: {Mathf.Max(0, playerStats.projectileBounces)}";
                if (lightningChainsText != null) lightningChainsText.text = $"Przeskoki pioruna: {playerStats.lightningChains}";
        if (allStatsText != null)
        {
            var p = playerStats;
            var move = p.GetComponent<TopDownPlayerController>();
            var ice = p.GetComponent<IceSpell>();
            allStatsText.text =
                $"<color=#DBC28D><b>ZDROWIE I MANA</b></color>\n" +
                $"HP: {p.currentHealth:F0} / {p.maxHealth:F0}     Regen: {p.healthRegen:F1}/s\n" +
                $"Mana: {p.currentMana:F0} / {p.maxMana:F0}     Regen: {p.manaRegen:F1}/s\n\n" +
                $"<color=#DBC28D><b>WALKA</b></color>\n" +
                $"Krytyk: {p.critChance:P0}     Siła krytyka: ×{p.critDamageMultiplier:F2}\n" +
                $"Kradzież życia: {p.lifesteal:P0}\n" +
                $"Odpychanie: ×{p.knockbackMultiplier:F2}\n" +
                $"Rozmiar / obszar: ×{p.sizeMultiplier:F2}\n\n" +
                $"<color=#DBC28D><b>POCISKI LODOWE</b></color>\n" +
                $"Pociski na rzut: {(ice ? ice.CurrentProjectileCount : 1 + p.extraProjectileCount)}\n" +
                $"Prędkość pocisków: ×{p.projectileSpeedMultiplier:F2}\n" +
                $"Odbicia na pocisk: {p.projectileBounces}\n" +
                $"Lód: {(ice ? ice.baseDamage * p.damageMultiplier : 0):F1} DMG | {(ice ? ice.CurrentInterval : 0):F2} s\n" +
                $"Piorun: {(stormSpell ? stormSpell.baseDamage * p.damageMultiplier : 0):F1} DMG | {(stormSpell ? stormSpell.CurrentInterval : 0):F2} s\n\n" +
                $"<color=#DBC28D><b>POSTAĆ</b></color>\n" +
                $"Ruch: ×{p.movementSpeedMultiplier:F2}  ({(move ? move.CurrentMoveSpeed : 0):F1} m/s)\n" +
                $"Zabójstwa: {p.kills}     Monety: {p.coins}\n" +
                $"<size=14>Odbicia i prędkość pocisków nie zmieniają pioruna.</size>";
        }
        bool canUpgrade = playerStats.unspentSkillPoints > 0;
        if (lightningChainsButton != null) lightningChainsButton.interactable = canUpgrade;
        if (attackSpeedButton != null) attackSpeedButton.interactable = canUpgrade;
        if (chainsButton != null) chainsButton.interactable = canUpgrade;
    }

    // Tę funkcję podepniesz pod przycisk [+] w Unity
    public void OnClickUpgradeMagic()
    {
        if (playerStats != null)
        {
            bool upgraded = playerStats.UpgradeMagicDamage();
            if (upgraded)
            {
                UpdateUI(); // Od razu odświeżamy cyfry na ekranie!
            }
        }
    }
    public void OnClickLevelUp()
    {
        if (playerStats != null && playerStats.ConfirmLevelUp())
            UpdateUI();
    }

    public void OnClickUpgradeAttackSpeed()
    {
        if (playerStats != null && playerStats.UpgradeLightningAttackSpeed()) UpdateUI();
    }

    public void OnClickUpgradeLightningChains()
    {
        if (playerStats != null && playerStats.UpgradeLightningChains()) UpdateUI();
    }

    public void OnClickUpgradeChains()
    {
        if (playerStats != null && playerStats.UpgradeProjectileBounces()) UpdateUI();
    }
}


