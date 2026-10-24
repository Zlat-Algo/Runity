using UnityEngine;

[DisallowMultipleComponent]
public class GIFAnimationPlayer : MonoBehaviour
{
    [SerializeField]
    private GIFAnimationClip clip;

    [Header("Targets")]
    [SerializeField]
    private SpriteRenderer spriteRenderer;

    [SerializeField]
    private UnityEngine.UI.Image image;

    [Header("Playback")]
    [SerializeField]
    private bool playOnEnable = true;

    private int currentFrame;
    private float timer;
    private bool isPlaying;

    public GIFAnimationClip Clip
    {
        get => clip;
        set => SetClip(value);
    }

    public bool IsPlaying => isPlaying;

    public int CurrentFrame => currentFrame;

    private void OnEnable()
    {
        if (playOnEnable)
            Play();
        else
            ShowFrame(0);
    }

    private void Update()
    {
        if (!isPlaying || clip == null || clip.FrameCount == 0)
            return;

        timer += Time.deltaTime;

        float frameDuration = 1f / clip.FramesPerSecond;

        while (timer >= frameDuration && isPlaying)
        {
            timer -= frameDuration;
            AdvanceFrame();
        }
    }

    public void Play()
    {
        if (clip == null || clip.FrameCount == 0)
        {
            isPlaying = false;
            return;
        }

        currentFrame = 0;
        timer = 0f;
        isPlaying = true;

        ShowFrame(currentFrame);
    }

    public void Pause()
    {
        isPlaying = false;
    }

    public void Stop()
    {
        isPlaying = false;
        timer = 0f;
        currentFrame = 0;

        ShowFrame(currentFrame);
    }

    public void SetClip(GIFAnimationClip newClip)
    {
        clip = newClip;
        currentFrame = 0;
        timer = 0f;

        if (isActiveAndEnabled && playOnEnable)
            Play();
        else
        {
            isPlaying = false;
            ShowFrame(0);
        }
    }

    public void SetFrame(int index)
    {
        if (clip == null || clip.FrameCount == 0)
            return;

        currentFrame = Mathf.Clamp(index, 0, clip.FrameCount - 1);
        ShowFrame(currentFrame);
    }

    private void AdvanceFrame()
    {
        int nextFrame = currentFrame + 1;

        if (nextFrame >= clip.FrameCount)
        {
            if (clip.Loop)
            {
                nextFrame = 0;
            }
            else
            {
                currentFrame = clip.FrameCount - 1;
                ShowFrame(currentFrame);
                isPlaying = false;
                return;
            }
        }

        currentFrame = nextFrame;
        ShowFrame(currentFrame);
    }

    private void ShowFrame(int index)
    {
        Sprite sprite = null;

        if (clip != null && clip.FrameCount > 0)
        {
            index = Mathf.Clamp(index, 0, clip.FrameCount - 1);
            sprite = clip.GetSprite(index);
        }

        if (spriteRenderer != null)
            spriteRenderer.sprite = sprite;

        if (image != null)
            image.sprite = sprite;
    }
}