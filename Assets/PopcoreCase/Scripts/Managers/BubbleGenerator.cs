using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;
using Sirenix.OdinInspector;

public class BubbleGenerator : Singleton<BubbleGenerator>
{
    [HideInInspector]
    public UnityEvent OnNewRowAdded = new UnityEvent();
    [HideInInspector]
    public UnityEvent OnMerge = new UnityEvent();

    [BoxGroup("Generation Information")]
    public float xOffset;
    [BoxGroup("Generation Information")]
    public int StartRowCount;
    [BoxGroup("Generation Information")]
    public int BubbleCountInARow;

    [BoxGroup("Bubble Information")]
    public string BubblePrefab;
    [BoxGroup("Bubble Information")]
    //This is hard coded becuase getting the information from the bubble would mean unnessary calculations at runtime.
    //Although calculations are not heavy and wouldn't make a difference there is no need to depend on a lower level object in the hierarcy.
    public float BubbleWidht = 0.5f;

    //This int will be set when a row created.
    //It can be only 1 or 0
    //0 means this is a reagular row so we will allign it at offset.
    //1 means this is an irregualr row so we will allign it at offset + half bubble width.
    public int rowType;

    public int moveQueue;

    private List<Bubble> BubblesInScene = new List<Bubble>();

    public List<Bubble> GetBubblesInScene { get { return BubblesInScene; } }

    private void Start()
    {
        GenerateRow(StartRowCount);
    }

    [Button]
    public void GenerateRow(int count)
    {
        if(moveQueue > 0) 
        {
            moveQueue += count;
            return;
        }


        for (int i = 0; i < count; i++)
        {
            GenerateRow();
        }
    }
   
    public void GenerateRow()
    {
        if(BubblesInScene.Count > 35)
        {
            isMovingDown = false;
            moveQueue = 0;
            return;
        }
        if (isMovingDown)
        {
            moveQueue++;
            return;
        }
        isMovingDown = true;
        Sequence sequence = DOTween.Sequence();
        BubbleThrower.Instance.disableInput = true;
        sequence
            .AppendCallback(MoveBubblesDown)
            .AppendInterval(0.2f)
            .AppendCallback(() =>
            {
                ResetHighestAllBubbles();
                for (int i = 0; i < BubbleCountInARow; i++)
                {
                    GameObject bubbleObject = PoolingSystem.Instance.InstantiateAPS(BubblePrefab, transform.position, transform);
                    DOTween.Kill(bubbleObject);
                    var bubblePos = transform.position;
                    bubblePos.x = (BubbleWidht * i) - xOffset;

                    if (rowType == 1)
                        bubblePos.x += BubbleWidht / 2;

                    bubbleObject.transform.position = bubblePos;
                    Bubble bubble = bubbleObject.GetComponent<Bubble>();
                    bubble.SetBubble(bubble.GenerateRandomLevel());
                    bubble.isStatinary = true;
                    bubble.isHighest = true;
                    AddBubble(bubble);
                }
                rowType = (rowType == 0) ? 1 : 0;
                OnNewRowAdded.Invoke();
                
            })
            .AppendInterval(0.1f)
            .AppendCallback(() =>
            {
                isMovingDown = false;
                ExecuteQueue();
                if(moveQueue <= 0)
                    BubbleThrower.Instance.disableInput = false;
            });
    }

    public void CheckLowestBubble()
    {
        float lowestBubblePoint = Mathf.Infinity;
        foreach (var bubble in BubblesInScene)
        {
            if (bubble.transform.position.y < lowestBubblePoint)
                lowestBubblePoint = bubble.transform.position.y;
        }

        if (lowestBubblePoint <= -2f)
            MoveGeneratorUp();

        if (lowestBubblePoint >= 2f)
        {
            if (transform.position.y > 5.4f)
            {
                transform.DOMoveY(5.4f, 0.5f);
            }
            else
                GenerateRow();
        }
    }

    private void MoveGeneratorUp()
    {
        transform.DOMoveY(transform.position.y + BubbleWidht, 0.3f);
    }


    public void ShoudlGenerateNewRow()
    {
        if (BubblesInScene.Count <= 25)
            GenerateRow(1);
    }

    private void ResetHighestAllBubbles()
    {
        foreach (var bubble in BubblesInScene)
        {
            bubble.isHighest = false;
        }
    }

    bool isMovingDown;

    private void MoveBubblesDown()
    {

        foreach (var bubble in BubblesInScene)
        {
            bubble.transform.DOMoveY(bubble.transform.position.y - BubbleWidht / 1.1f, 0.1f).OnComplete(()=>
            {
                isMovingDown = false;
            });
        }
    }

    private void ExecuteQueue()
    {
        if(moveQueue <= 0)
        {
            moveQueue = 0;
            return;
        }
        moveQueue--;
        GenerateRow();
    }

    public void AddBubble(Bubble bubble)
    {
        if(!BubblesInScene.Contains(bubble))
            BubblesInScene.Add(bubble);

        bubble.transform.SetParent(transform);
    }

    public void RemoveBubble(Bubble bubble)
    {
        if(BubblesInScene.Contains(bubble))
            BubblesInScene.Remove(bubble);
    }

    private void Update()
    {
        //This is an ugly way of doing this 
        if(MergeChain.Count > 0)
        {
            if(Time.time > lastTimeBubbleAdded + 0.1f)
            {
                MergeBubbles();
            }
        }
    }

    public List<Bubble> MergeChain = new List<Bubble>();
    float lastTimeBubbleAdded;

    public void AddToChain(Bubble bubble)
    {
        if (!MergeChain.Contains(bubble))
        {
            MergeChain.Add(bubble);
            lastTimeBubbleAdded = Time.time;
        }
    }

    public void MergeBubbles()
    {
        if (MergeChain.Count == 0)
            return;

        
        Bubble bestBubble = HighestBubble(MergeChain);

        for (int i = 0; i < MergeChain.Count; i++)
        {
            if (MergeChain[i].ValidateMerge(MergeChain.Count))
                bestBubble = MergeChain[i];
        }
        MergeChain.Remove(bestBubble);

        bestBubble.mergedBubbles = new List<Bubble>(MergeChain);
        bestBubble.MergeTo();
        MergeChain.Clear();
    }

    private Bubble HighestBubble(List<Bubble> bubbles)
    {
        Bubble highestBubble = MergeChain[0];
        for (int i = 0; i < MergeChain.Count; i++)
        {
            if (MergeChain[i].transform.position.y > highestBubble.transform.position.y)
                highestBubble = MergeChain[i];
        }
        return highestBubble;
    }

    public List<Bubble> GetNeighbors(Vector2 pos)
    {
        List<Bubble> neighbors = new List<Bubble>();

        for (int i = 0; i < BubblesInScene.Count; i++)
        {
            if (Vector2.Distance(BubblesInScene[i].transform.position, pos) <= BubbleWidht * 1.2f)
            {
                if(!neighbors.Contains(BubblesInScene[i]))
                    neighbors.Add(BubblesInScene[i]);
            }
        }
        return neighbors;
    }
}