using TMPro;
using Unity.Collections;
using UnityEngine;

public class PlayerHeadUI : MonoBehaviour
{
    [SerializeField] private PlayerNetworkState target;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private RectTransform hpFillRect;
    [SerializeField] private float maxBarWidth = 100f;

    private void Start()
    {
        if (target == null)
            target = GetComponentInParent<PlayerNetworkState>();

        if (target == null)
        {
            Debug.LogError($"{gameObject.name} µÄ PlayerHeadUI ÕÒ²»µ½ PlayerNetworkState");
            return;
        }

        if (target.IsOwner)
        {
            gameObject.SetActive(false);
            return;
        }

        RefreshAll();

        target.PlayerName.OnValueChanged += OnNameChanged;
        target.HP.OnValueChanged += OnHpChanged;
    }

    private void LateUpdate()
    {
        Camera cam = LocalPlayerCamera.LocalCam;
        if (cam == null) return;

        Vector3 dir = cam.transform.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f) return;

        transform.forward = -dir.normalized;
    }

    private void OnDestroy()
    {
        if (target == null) return;

        target.PlayerName.OnValueChanged -= OnNameChanged;
        target.HP.OnValueChanged -= OnHpChanged;
    }

    private void OnNameChanged(FixedString32Bytes oldValue, FixedString32Bytes newValue)
    {
        nameText.text = newValue.ToString();
    }

    private void OnHpChanged(int oldValue, int newValue)
    {
        RefreshHPBar();
    }

    private void RefreshAll()
    {
        nameText.text = target.PlayerName.Value.ToString();
        RefreshHPBar();
    }

    private void RefreshHPBar()
    {
        if (hpFillRect == null) return;
        if (target.MaxHP <= 0) return;

        float ratio = (float)target.HP.Value / target.MaxHP;
        ratio = Mathf.Clamp01(ratio);

        Vector2 size = hpFillRect.sizeDelta;
        size.x = maxBarWidth * ratio;
        hpFillRect.sizeDelta = size;
    }
}