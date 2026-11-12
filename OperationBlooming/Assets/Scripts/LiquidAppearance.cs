
using UnityEngine;

[DefaultExecutionOrder(400)]
public class LiquidAppearance : MonoBehaviour
{
	[Header("Renderers")]
	[SerializeField] private Renderer liquidRenderer;
	[SerializeField] private Renderer surfaceRenderer;

	[Header("Liquid Appearance")]
	[SerializeField] private Color liquidColor = Color.blue;

	[Range(0f, 1f)]
	[SerializeField] private float transparency = 0.8f;

	private MaterialPropertyBlock propertyBlock;

	private Color previousColor;
	private float previousTransparency;
	private bool needsUpdate = true;

	private static readonly int ColorId =
		Shader.PropertyToID("_LiquidColor");

	private static readonly int TransparencyId =
		Shader.PropertyToID("_Transparency");

	private void Awake()
	{
		propertyBlock = new MaterialPropertyBlock();
	}

	private void OnEnable()
	{
		needsUpdate = true;
	}

	private void LateUpdate()
	{
		if (liquidColor != previousColor ||
			!Mathf.Approximately(
				transparency, previousTransparency))
		{
			needsUpdate = true;
		}

		if (needsUpdate)
			ApplyAppearance();
	}

	public void SetColor(Color newColor)
	{
		liquidColor = newColor;
		needsUpdate = true;

		ApplyAppearance();
	}

	public void SetTransparency(float value)
	{
		transparency = Mathf.Clamp01(value);
		needsUpdate = true;

		ApplyAppearance();
	}

	public void SetAppearance(Color color, float alpha)
	{
		liquidColor = color;
		transparency = Mathf.Clamp01(alpha);

		needsUpdate = true;
		ApplyAppearance();
	}

	public Color GetColor()
	{
		return liquidColor;
	}

	public float GetTransparency()
	{
		return transparency;
	}

	private void ApplyAppearance()
	{
		if (propertyBlock == null)
			propertyBlock = new MaterialPropertyBlock();

		UpdateRenderer(liquidRenderer);

		if (surfaceRenderer != liquidRenderer)
			UpdateRenderer(surfaceRenderer);

		previousColor = liquidColor;
		previousTransparency = transparency;
		needsUpdate = false;
	}

	private void UpdateRenderer(Renderer targetRenderer)
	{
		if (targetRenderer == null)
			return;

		targetRenderer.GetPropertyBlock(propertyBlock);

		propertyBlock.SetColor(ColorId, liquidColor);

		propertyBlock.SetFloat(
			TransparencyId, transparency
		);

		targetRenderer.SetPropertyBlock(propertyBlock);
	}
}
