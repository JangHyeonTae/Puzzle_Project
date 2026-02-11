using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using System.Linq;

public class StageClearAnim : MonoBehaviour
{
    [SerializeField] private float animDelay = 0.03f;
    [SerializeField] private float animDuration = 0.4f;

    private GameObject root;
    private List<Transform> blocks = new List<Transform>();

    public async UniTaskVoid PlayClearEffect()
    {
        ResetRoot();

        CreateBlocksFromStage();

        if (blocks.Count == 0) return;

        var sortedBlocks = blocks
            .OrderByDescending(t => t.position.x + t.position.y)
            .ToList();

        await PlayWaveAnimation(sortedBlocks);

        await FinishEffect();
    }

    private void ResetRoot()
    {
        if (root != null)
            Destroy(root);

        blocks.Clear();
        root = new GameObject("StageClearRoot");
    }

    private void CreateBlocksFromStage()
    {
        StagePrefab stage = DrawGrid.Instance.stagePrefab;
        if (stage == null) return;

        Vector3 cellSize = DrawGrid.Instance.grid.cellSize;

        var positions = stage.BlockPositions;
        var sprites = stage.BlockSprites;

        for (int i = 0; i < positions.Count; i++)
        {
            GameObject go = new GameObject("AnimBlock");
            go.transform.SetParent(root.transform);
            go.transform.position = positions[i];

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprites[i];
            sr.sortingOrder = 101;
            sr.color = DrawGrid.Instance.highlightColor;

            float spriteSize = sr.bounds.size.x;
            float scale = cellSize.x / spriteSize * 1.2f;
            go.transform.localScale = Vector3.one * scale;

            blocks.Add(go.transform);
        }
    }

    private async UniTask PlayWaveAnimation(List<Transform> sortedList)
    {
        for (int i = 0; i < sortedList.Count; i++)
        {
            sortedList[i]
                .DOPunchScale(Vector3.one * 0.55f, animDuration, 2, 0.5f);

            await UniTask.Delay(System.TimeSpan.FromSeconds(animDelay));
        }

        await UniTask.Delay(System.TimeSpan.FromSeconds(animDuration));
    }

    private async UniTask FinishEffect()
    {
        foreach (var t in blocks)
        {
            var sr = t.GetComponent<SpriteRenderer>();
            sr.DOFade(0f, 0.3f);
        }

        await UniTask.Delay(300);

        Destroy(root);
    }
}
