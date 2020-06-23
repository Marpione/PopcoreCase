using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using Sirenix.OdinInspector;
using DG.Tweening;
public class Bubble : MonoBehaviour
{
    public List<BubbleData> BubbleData = new List<BubbleData>();

    private SpriteRenderer spriteRenderer;
    private SpriteRenderer SpriteRenderer { get { return (spriteRenderer == null) ? spriteRenderer = GetComponentInChildren<SpriteRenderer>() : spriteRenderer; } }

    private TextMeshProUGUI bubbleText;
    private TextMeshProUGUI BubbleText { get { return (bubbleText == null) ? bubbleText = GetComponentInChildren<TextMeshProUGUI>() : bubbleText; } }

    public int CurrentBubbleLevel;

    public List<Bubble> connectedBubbles = new List<Bubble>();

    public bool isStatinary;

    public bool isHighest;

    private CircleCollider2D circleCollider;
    public CircleCollider2D CircleCollider2D { get { return (circleCollider == null) ? circleCollider = GetComponent<CircleCollider2D>() : circleCollider; } }

    private Rigidbody2D rigidbody2D;
    private Rigidbody2D Rigidbody2D { get { return (rigidbody2D == null) ? rigidbody2D = GetComponent<Rigidbody2D>() : rigidbody2D; } }

    Transform graphic;
    Transform Graphic { get { return (graphic == null) ? graphic = transform.Find("Graphic") : graphic; } }

    private Vector3 lastContactDir;

    private void OnEnable()
    {
        CircleCollider2D.isTrigger = false;
        connectedBubbles.Clear();
        BubbleGenerator.Instance.OnNewRowAdded.AddListener(CheckNeighbors);
        BubbleGenerator.Instance.OnMerge.AddListener(CheckFall);
    }

    private void OnDisable()
    {
        BubbleGenerator.Instance.OnNewRowAdded.RemoveListener(CheckNeighbors);
        BubbleGenerator.Instance.OnMerge.RemoveListener(CheckFall);
    }

    public void CheckNeighbors()
    {
        connectedBubbles.Clear();
        //This is not optimized since we call this method on multiple objects very often but this is the fastes and most optimal solution for this case.
        Collider2D[] collidersInRange = Physics2D.OverlapCircleAll(transform.position, BubbleGenerator.Instance.BubbleWidht * 1.2f);

        foreach (var col in collidersInRange)
        {
            Bubble bubble = col.GetComponent<Bubble>();
            if(bubble)
            {
                bubble.lastContactDir = transform.position - bubble.transform.position;
                AddNeighbor(bubble);
            }
        }
    }

    private void AddNeighbor(Bubble bubble)
    {
        if (ReferenceEquals(bubble, this))
            return;
        if (bubble != isStatinary)
            return;

        if (!connectedBubbles.Contains(bubble))
            connectedBubbles.Add(bubble);
    }

    private void RemoveNeighbor(Bubble bubble)
    {
        if (connectedBubbles.Contains(bubble))
            connectedBubbles.Remove(bubble);
    }

    public int GenerateRandomLevel()
    {
        //The range a bubble can have when created is 6. This is hardcoded for the demo but it can also be extracted to a 
        //variable for game designers to play around.
        return Random.Range(0, 6);
    }

    
    public void SetBubble()
    {
        SetBubble(CurrentBubbleLevel);
    }

    //I create this variable here becasue it's only considering the method below.
    //A little bit unorthodox but just a personal preferance
    private bool isTweening;


    public void SetBubble(int level)
    {
        if(level >= BubbleData.Count)
        {
            CurrentBubbleLevel = BubbleData.Count - 1;
            ExplodeBubble();
            return;
        }
        //There are checks in dotween for checking if the tween is happening
        //However I find that sometimes they don't work as expected or I set them incorrectly.
        //This is dirty but I prefer to use this workaround.
        if(!isTweening)
        {
            isTweening = true;
            Graphic.DOPunchScale(Graphic.localScale * 0.2f, 0.2f).OnComplete(() => isTweening = false);
        }

        SpriteRenderer.color = BubbleData[level].LevelColor;
        BubbleText.SetText(BubbleData[level].LevelNumber.ToString());
        CurrentBubbleLevel = level;
    }

    public void ExplodeBubble()
    {
        //DestroyBubble modifies the connectedBubbles list. To avoid errors we simply clone the list
        List<Bubble> connectedBubbleClone = new List<Bubble>(connectedBubbles);

        for (int i = 0; i < connectedBubbleClone.Count; i++)
        {
            BubbleEffect bubbleEffect = PoolingSystem.Instance.InstantiateAPS("BubblePopEffect", connectedBubbleClone[i].transform.position).GetComponent<BubbleEffect>();
            bubbleEffect.SetParticleColor(BubbleData[connectedBubbleClone[i].CurrentBubbleLevel].LevelColor);
            connectedBubbleClone[i].DestroyBubble();
        }


        Camera.main.transform.DOShakePosition(0.2f, 0.7f, 2).OnComplete(() => Camera.main.transform.position = new Vector3(0, 0, -10));
        DestroyBubble();
    }


    public void CheckForMerge()
    {
        CheckNeighbors();

        for (int i = 0; i < connectedBubbles.Count; i++)
        {
            if(connectedBubbles[i].CurrentBubbleLevel == CurrentBubbleLevel)
            {
                if(!BubbleGenerator.Instance.MergeChain.Contains(connectedBubbles[i]))
                {
                    BubbleGenerator.Instance.AddToChain(connectedBubbles[i]);
                    connectedBubbles[i].CheckForMerge();
                }

            }
        }
    }

    public bool ValidateMerge(int level)
    {
        CheckNeighbors();

        int targetLevel = CurrentBubbleLevel + level;

        for (int i = 0; i < connectedBubbles.Count; i++)
        {
            if (connectedBubbles[i].CurrentBubbleLevel == targetLevel)
            {
                return true;
            }
        }

        return false;
    }

    //Becuse dotween is async sending the list with the method wouldn't work same goes for coroutines
    //This is why we are setting the list outside of the method and clearing the bubbles after they reach their destination.
    //I create this variable here becasue it's only considering the method below.
    //A little bit unorthodox but just a personal preferance
    [HideInInspector]
    public List<Bubble> mergedBubbles = new List<Bubble>();

    public void MergeTo()
    {
        CurrentBubbleLevel += mergedBubbles.Count;
        bool shouldExplode = (mergedBubbles.Count >= 3) ? true : false;
        for (int i = 0; i < mergedBubbles.Count; i++)
        {
            BubbleEffect bubbleEffect = PoolingSystem.Instance.InstantiateAPS("BubblePopEffect", mergedBubbles[i].transform.position).GetComponent<BubbleEffect>();
            bubbleEffect.SetParticleColor(mergedBubbles[i].BubbleData[mergedBubbles[i].CurrentBubbleLevel].LevelColor);
            mergedBubbles[i].transform.DOMove(transform.position, 0.2f).OnComplete(() =>
            {
                BubbleGenerator.Instance.OnMerge.Invoke();
                SetBubble();
                DestroyMergedBubbles();
                CheckForMerge();
                if (shouldExplode)
                    ExplodeBubble();
            });
        }
    }

    //Actually we are calling this method multiple times but we are clearing the list after the 
    //first call so other calls will not execute anything.
    void DestroyMergedBubbles()
    {
        foreach (var bubble in mergedBubbles)
        {
            bubble.DestroyBubble();
        }
        mergedBubbles.Clear();
    }

    public void CheckFall()
    {
        if (!isStatinary)
            return;

        if (isHighest)
            return;

        CheckNeighbors();

        for (int i = 0; i < connectedBubbles.Count; i++)
        {
            if (connectedBubbles[i].transform.position.y > transform.position.y)
                return;
        }

        //To prevent stackoverflow
        CircleCollider2D.enabled = false;

        foreach (var connectedBubbles in connectedBubbles)
        {
            connectedBubbles.RemoveNeighbor(this);
            //Causin stackoverflow if collider is enabled.
            connectedBubbles.CheckFall();
        }

        DoFall();
    }

    private void DoFall()
    {
        CircleCollider2D.enabled = true;
        CircleCollider2D.isTrigger = true;
        Rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
        Rigidbody2D.AddForce(lastContactDir, ForceMode2D.Impulse);
    }

    public void DestroyBubble()
    {
        transform.position = PoolingSystem.Instance.transform.position;
        BubbleGenerator.Instance.RemoveBubble(this);
        for (int i = 0; i < connectedBubbles.Count; i++)
        {
            connectedBubbles[i].RemoveNeighbor(this);
        }
        BubbleGenerator.Instance.OnNewRowAdded.RemoveListener(CheckNeighbors);
        BubbleGenerator.Instance.OnMerge.RemoveListener(CheckFall);
        isStatinary = false;
        CircleCollider2D.isTrigger = false;
        BubbleGenerator.Instance.ShoudlGenerateNewRow();
        Rigidbody2D.velocity = Vector2.zero;
        Rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
        PoolingSystem.Instance.DestroyAPS(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        BubbleDestroyer bubbleDestroyer = other.GetComponent<BubbleDestroyer>();

        if (bubbleDestroyer)
        {
            BubbleEffect bubbleEffect = PoolingSystem.Instance.InstantiateAPS("BubblePopEffect", transform.position).GetComponent<BubbleEffect>();
            bubbleEffect.SetParticleColor(BubbleData[CurrentBubbleLevel].LevelColor);
            DestroyBubble();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, BubbleGenerator.Instance.BubbleWidht * 1.2f);
    }
}
