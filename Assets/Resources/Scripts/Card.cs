using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate void ClickCardHandler(Card card);
public delegate void EnterCardHandler(Card card);
public delegate void ExitCardHandler(Card card);

public enum CardType
{
    Train,
    Plane,
    Rocket,
    Hyperloop
}

public class Card : MonoBehaviour
{
    public static Dictionary<int, CardType> cardIndexTypeMap = new Dictionary<int, CardType>
    {
        { 0, CardType.Train },
        { 1, CardType.Plane },
        { 2, CardType.Rocket },
        { 3, CardType.Hyperloop },
    };
    public static Dictionary<CardType, int> cardTypeIndexMap = new Dictionary<CardType, int>
    {
        { CardType.Train, 0 },
        { CardType.Plane, 1 },
        { CardType.Rocket, 2 },
        { CardType.Hyperloop, 3 },
    };
    
    public CardType cardType;
    public int playerId;
    public ClickCardHandler clickHandler = null;
    public EnterCardHandler enterHandler = null;
    public ExitCardHandler exitHandler = null;

    public int routeCost = 0;
    public int peepsMoved = 0;
    public int routeDistance = 0;

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void PlaceText(GameObject uiInfoText)
    {
        string info = "";

        string name = "";
        if (cardType == CardType.Train) { name = "TRAIN..."; }
        if (cardType == CardType.Plane) { name = "PLANE"; }
        if (cardType == CardType.Rocket) { name = "ROCKET!"; }
        if (cardType == CardType.Hyperloop) { name = "HYPERLOOP?!"; }

        info = "<size=55><color=red><b>" + name + "</b></color></size>\n";
        info += "<color=green>$" + routeCost + "</color> per route\n";
        info += "<color=teal>" + peepsMoved + "</color> peeps moved per turn\n";

        if (routeDistance == 1)
        {
            info += "<color=yellow>" + routeDistance + "</color> station apart at most";
        }
        else
        {
            info += "<color=yellow>" + routeDistance + "</color> stations apart at most";
        }

        var text = uiInfoText.GetComponentInChildren<UnityEngine.UI.Text>();
        text.text = info;
    }

    public void OnMouseEnter()
    {
        if (enterHandler != null)
        {
            enterHandler(this);
        }
    }


    public void OnMouseExit()
    {
        if (exitHandler != null)
        {
            exitHandler(this);
        }
    }

    public void Clicked()
    {
        if (clickHandler != null)
        {
            clickHandler(this);
        }
    }
}
