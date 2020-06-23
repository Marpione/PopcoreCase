using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Sirenix.OdinInspector;

public class BubbleThrower : Singleton<BubbleThrower>
{
    public Transform EquipedBubblePoint;
    public Transform SecondBubblePoint;

    public GameObject BubbleIndicator;

    LineRenderer lineRenderer;
    LineRenderer LineRenderer { get { return (lineRenderer == null) ? lineRenderer = GetComponent<LineRenderer>() : lineRenderer; } }

    private Bubble equipedBubble;
    private Bubble secondBubble;

    public bool disableInput;

    public LayerMask RaycastLayers;



    private void Start()
    {
        CreateBubble();
    }

    private void OnMouseDown()
    {
        SwitchBubbles();
    }

    private void Update()
    {
        if (disableInput)
        {
            BubbleIndicator.SetActive(false);
            return;
        }

        GetDestination(PointClickInput.Instance.GetMousePosition(EquipedBubblePoint.position));
        SetVisuals();
        if (Input.GetMouseButtonUp(0))
            ThrowBubble(PointClickInput.Instance.GetMousePosition(EquipedBubblePoint.position));
    }


    private List<Vector3> GetDestination(Vector2 inputPosition)
    {
        Vector2 snapPoint = Vector2.zero;
        var ray = new Ray2D(EquipedBubblePoint.position, inputPosition);

        List<Vector3> path = new List<Vector3>();


        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction, Mathf.Infinity, RaycastLayers);


        if (hit)
        {
            Debug.DrawLine(EquipedBubblePoint.position, hit.centroid, Color.red);
            //I usually don't use tags too often but in this case it's faster than cheking by the component
            //and here we don't need to get the component.

            //Shot is directly to bubbles
            if (hit.collider.tag == "Bubble")
            {
                path.Add(ray.origin);
                snapPoint = GetSnapPoint(hit.normal, hit.collider.transform.position);
                path.Add(snapPoint);
                lastHitPoint = hit.point;
            }
            //Shot Bounces from wall
            else if (hit.collider.tag == "Wall")
            {

                // Get a rotation to go from our ray direction (negative, so coming from the wall),
                // to the normal of whatever surface we hit.
                var deflectRotation = Quaternion.FromToRotation(-ray.direction, hit.normal);

                // We then take that rotation and apply it to the same normal vector to basically
                // mirror that angle difference.
                var deflectDirection = deflectRotation * hit.normal;

                Ray deflectRay = new Ray(hit.centroid, deflectDirection);

                //When using a single cast hit retuns the original rays hit(Wall) we will just do a for loop in hits an see if we hit a bubble.
                RaycastHit2D[] bounceHit = Physics2D.RaycastAll(deflectRay.origin, deflectRay.direction, Mathf.Infinity, RaycastLayers);
                

                //I would impliment a bounce method to support multible wall hits
                //Kind of zigzag movement but in example game 2 wall bounce used as cancellesion method
                //this is why I'm doing the same
                RaycastHit2D bubbleHit = new RaycastHit2D();
                for (int i = 0; i < bounceHit.Length; i++)
                {
                    if (bounceHit[i].collider.tag == "Bubble")
                    {
                        Debug.DrawLine(deflectRay.origin, bounceHit[i].point, Color.blue);
                        path.Add(ray.origin);
                        path.Add(deflectRay.origin);
                        bubbleHit = bounceHit[i];
                        lastHitPoint = bubbleHit.point;
                        break;
                    }
                }
                if(bubbleHit)
                {
                    snapPoint = GetSnapPoint(bubbleHit.normal, bubbleHit.transform.position);
                    path.Add(snapPoint);
                }
                
            }
        }

        return path;
    }

    //We use the path for everything but line must end where last hit location happend.
    //We can out the last hit point in the method but it would mean there will be a lot of unnecasy out vars.
    //So we extract it to this variable;
    Vector2 lastHitPoint;

    public void SetVisuals()
    {
        List<Vector3> path = GetDestination(PointClickInput.Instance.GetMousePosition(EquipedBubblePoint.position));
        if (Input.GetMouseButton(0))
        {
            if(path.Count > 0)
            {
                SetLine(path);
                BubbleIndicator.transform.position = path[path.Count - 1];
                BubbleIndicator.SetActive(true);
            }
            else
            {
                LineRenderer.positionCount = 0;
                BubbleIndicator.SetActive(false);
            }
        }
        else
        {
            LineRenderer.positionCount = 0;
            BubbleIndicator.SetActive(false);
        }
    }

    private void SetLine(List<Vector3> path)
    {
        LineRenderer.positionCount = 0;
        LineRenderer.positionCount = path.Count;
        for (int i = 0; i < path.Count; i++)
        {
            LineRenderer.SetPosition(i, path[i]);
        }

        LineRenderer.SetPosition(LineRenderer.positionCount - 1, lastHitPoint);
    }

    [Button]
    public void CreateBubble()
    {
        if(equipedBubble == null)
        {
            if (secondBubble == null)
            {
                //TODO: Tight this Up
                equipedBubble = PoolingSystem.Instance.InstantiateAPS("Bubble", EquipedBubblePoint.position).GetComponent<Bubble>();
                secondBubble = PoolingSystem.Instance.InstantiateAPS("Bubble", SecondBubblePoint.position).GetComponent<Bubble>();
                equipedBubble.isStatinary = false;
                equipedBubble.gameObject.layer = 10;
                equipedBubble.SetBubble(equipedBubble.GenerateRandomLevel());
                secondBubble.transform.localScale = Vector2.one / 1.5f;
                secondBubble.gameObject.layer = 10;
                secondBubble.isStatinary = false;
                secondBubble.SetBubble(secondBubble.GenerateRandomLevel());
            }
            else
            {
                //TODO: Tight this Up
                equipedBubble = secondBubble;
                secondBubble.transform.DOMove(EquipedBubblePoint.position, 0.5f);
                secondBubble.transform.DOScale(Vector3.one, 0.5f);
                secondBubble = PoolingSystem.Instance.InstantiateAPS("Bubble", SecondBubblePoint.position).GetComponent<Bubble>();
                secondBubble.gameObject.layer = 10;
                secondBubble.transform.localScale = Vector2.one / 1.5f;
                secondBubble.isStatinary = false;
                secondBubble.SetBubble(secondBubble.GenerateRandomLevel());
            }
        }
    }

    private void SwitchBubbles()
    {
        if (disableInput)
            return;

        disableInput = true;
        Sequence sequence = DOTween.Sequence();
        sequence
            .Append(secondBubble.transform.DOScale(Vector3.one, 0.3f))
            .Join(equipedBubble.transform.DOScale(Vector3.one / 1.5f, 0.3f))
            .Append(equipedBubble.transform.DOMove(SecondBubblePoint.position, 0.3f))
            .Join(secondBubble.transform.DOMove(EquipedBubblePoint.position, 0.3f))
            .OnComplete(() =>
            {
                disableInput = false;
                var tempBubble = secondBubble;
                secondBubble = equipedBubble;
                equipedBubble = tempBubble;
            });
    }

    private void ThrowBubble(Vector2 destination)
    {
        List<Vector3> path = GetDestination(destination);
        if (path.Count == 0)
            return;

        equipedBubble.gameObject.layer = 9;
        disableInput = true;
        equipedBubble.isStatinary = true;
        equipedBubble.transform.DOPath(path.ToArray(), 0.5f)
            .OnComplete(()=> {
                disableInput = false;
                equipedBubble.CheckForMerge();
                BubbleGenerator.Instance.AddBubble(equipedBubble);
                BubbleGenerator.Instance.CheckLowestBubble();
                equipedBubble = null;
                CreateBubble();
                });
    }

    public LayerMask BubbleMask;
    Vector3 GetSnapPoint(Vector2 normal, Vector2 hitObjectPos)
    {
        Vector2 snapPoint = hitObjectPos;
        float bubbleWidht = BubbleGenerator.Instance.BubbleWidht;

        List<Vector2> possiblePoints = new List<Vector2>();

        // Left and Right
        Vector2 right = hitObjectPos + Vector2.right * bubbleWidht;
        Vector2 left = hitObjectPos + Vector2.left * bubbleWidht;

        //Down Left and Right
        Vector2 lowerRight = new Vector2(hitObjectPos.x + bubbleWidht / 2, hitObjectPos.y - bubbleWidht / 1.1f);
        Vector2 lowerLeft = new Vector2(hitObjectPos.x - bubbleWidht / 2, hitObjectPos.y - bubbleWidht / 1.1f);

        //Upper Left and Right
        Vector2 upperRight = new Vector2(hitObjectPos.x + bubbleWidht / 2, hitObjectPos.y + bubbleWidht / 1.1f);
        Vector2 upperLeft = new Vector2(hitObjectPos.x - bubbleWidht / 2, hitObjectPos.y + bubbleWidht / 1.1f);

        //This is a really long if statement I usually avoid this kind of coding but in the case I had no choice and time is sort
        //I'm not great at math so I created a working logic. I tried better solutions but this on is working the bets for UX.
        //LowerHit
        if(normal.y < 0)
        {
            //Right
            if (normal.x > 0)
            {
                if (!IsOccupied(lowerRight))
                    snapPoint = lowerRight;
                else
                {
                    if (!IsOccupied(right))
                        snapPoint = right;
                    else
                    {
                        if (!IsOccupied(lowerLeft))
                            snapPoint = lowerLeft;
                        else
                        {
                            if (!IsOccupied(left))
                                snapPoint = left;  
                        }
                    }
                }
            }
            //Left
            else
            {
                if (!IsOccupied(lowerLeft))
                    snapPoint = lowerLeft;
                else
                {
                    if (!IsOccupied(left))
                        snapPoint = left;
                    else
                    {
                        if (!IsOccupied(lowerRight))
                            snapPoint = lowerRight;
                        else
                        {
                            if (!IsOccupied(right))
                                snapPoint = right;
                        }
                    }
                }
            }
        }
        //UpperHit
        else
        {
            //Right
            if (normal.x > 0)
            {
                if (!IsOccupied(upperRight))
                    snapPoint = upperRight;
                else
                {
                    if (!IsOccupied(right))
                        snapPoint = right;
                    else
                    {
                        if (!IsOccupied(upperLeft))
                            snapPoint = upperLeft;
                        else
                        {
                            if (!IsOccupied(left))
                                snapPoint = left;
                        }
                    }
                }
            }
            else
            {
                if (!IsOccupied(upperLeft))
                    snapPoint = upperLeft;
                else
                {
                    if (!IsOccupied(left))
                        snapPoint = left;
                    else
                    {
                        if (!IsOccupied(upperRight))
                            snapPoint = upperRight;
                        else
                        {
                            if (!IsOccupied(right))
                                snapPoint = right;
                        }
                    }
                }
            }
        }

        if(normal.y == 0)
        {
            if (normal.x > 0)
                snapPoint = right;
            else snapPoint = left;
        }

        return snapPoint;
    }

    private bool IsOccupied(Vector2 pos)
    {
        return Physics2D.OverlapCircle(pos, BubbleGenerator.Instance.BubbleWidht / 2.2f, BubbleMask);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(BubbleIndicator.transform.position, BubbleGenerator.Instance.BubbleWidht / 2.2f);
    }
}
