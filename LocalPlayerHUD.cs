using TMPro;
using Unity.Collections;
using UnityEngine;

public class LocalPlayerHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text selfNameText;
    [SerializeField] private TMP_Text selfHpValueText;   // ¿É²»ÍÏ
    [SerializeField] private RectTransform hpFillRect;
    [SerializeField] private float maxBarWidth = 200f;

    private PlayerNetworkState localPlayerState;

    public void Bind(PlayerNetworkState state)
    {
        if (localPlayerState != null)
        {
            localPlayerState.PlayerName.OnValueChanged -= OnNameChanged;
            localPlayerState.HP.OnValueChanged -= OnHpChanged;
        }

        localPlayerState = state;

        if (localPlayerState == null) return;

        RefreshAll();

        localPlayerState.PlayerName.OnValueChanged += OnNameChanged;
        localPlayerState.HP.OnValueChanged += OnHpChanged;
    }

    private void OnDestroy()
    {
        if (localPlayerState == null) return;

        localPlayerState.PlayerName.OnValueChanged -= OnNameChanged;
        localPlayerState.HP.OnValueChanged -= OnHpChanged;
    }

    private void OnNameChanged(FixedString32Bytes oldValue, FixedString32Bytes newValue)
    {
        if (selfNameText != null)
            selfNameText.text = newValue.ToString();
    }

    private void OnHpChanged(int oldValue, int newValue)
    {
        RefreshHPBar();
    }

    private void RefreshAll()
    {
        if (selfNameText != null)
            selfNameText.text = localPlayerState.PlayerName.Value.ToString();

        RefreshHPBar();
    }

    private void RefreshHPBar()
    {
        if (localPlayerState == null) return;
        if (hpFillRect == null) return;
        if (localPlayerState.MaxHP <= 0) return;

        float ratio = (float)localPlayerState.HP.Value / localPlayerState.MaxHP;
        ratio = Mathf.Clamp01(ratio);

        Vector2 size = hpFillRect.sizeDelta;
        size.x = maxBarWidth * ratio;
        hpFillRect.sizeDelta = size;

        if (selfHpValueText != null)
            selfHpValueText.text = $"{localPlayerState.HP.Value}/{localPlayerState.MaxHP}";
    }
}