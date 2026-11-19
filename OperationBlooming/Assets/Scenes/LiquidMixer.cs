using UnityEngine;

[RequireComponent(typeof(LiquidAppearance))]
[RequireComponent(typeof(LiquidVolumeController))]
public class LiquidMixer : MonoBehaviour
{
    private LiquidAppearance appearance;
    private LiquidVolumeController volume;

    private void Awake()
    {
        appearance = GetComponent<LiquidAppearance>();
        volume = GetComponent<LiquidVolumeController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        LiquidMixer otherMixer = other.GetComponentInParent<LiquidMixer>();

        if (otherMixer == null || otherMixer == this ||
            !otherMixer.isActiveAndEnabled)
        {
            return;
        }

        float ownAmount = volume.FillFraction;
        float otherAmount = otherMixer.volume.FillFraction;

        if (ownAmount <= 0f || otherAmount <= 0f)
        {
            return;
        }

        Color mixedColor = Color.Lerp(
            appearance.GetColor(),
            otherMixer.appearance.GetColor(),
            otherAmount / (ownAmount + otherAmount)
        );

        appearance.SetColor(mixedColor);
        otherMixer.appearance.SetColor(mixedColor);
    }
}