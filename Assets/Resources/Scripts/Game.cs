using System.Collections;
using System.Collections.Generic;
using UnityEngine;

enum SelectionMode
{
    Intro,
    Card,
    RouteFromStation,
    RouteToStation,
    CPUMove,
    GameOver
}

public struct RouteSelection
{
    public Player player;
    public StationInfo fromStation;
    public StationInfo toStation;
    public Card routeType;
}

public class Route
{
    public Player player;
    public GameObject routeTypeObj = null;
    public StationInfo fromStation;
    public StationInfo toStation;
    public Card routeType;
    Dictionary<CardType, GameObject> prefabMap;

    public Route(RouteSelection route, Dictionary<CardType, GameObject> prefabMap)
    {
        CreateRoute(route.player, route.fromStation, route.toStation, route.routeType, prefabMap);
    }

    //public Route(Player player, StationInfo fromStation, StationInfo toStation, Card routeType, Dictionary<CardType, GameObject> prefabMap)
    //{
    //    CreateRoute(player, fromStation, toStation, routeType, prefabMap);
    //}

    private void CreateRoute(Player player, StationInfo fromStation, StationInfo toStation, Card routeType, Dictionary<CardType, GameObject> prefabMap)
    {
        this.player = player;
        this.fromStation = fromStation;
        this.toStation = toStation;
        this.prefabMap = prefabMap;

        // generate route prefab of correct player colour in correct place
        MakeRoute(player, routeType);
    }
    
    public void MakeRoute(Player player, Card newRouteType)
    {
        if (routeTypeObj != null) { GameObject.Destroy(routeTypeObj); }

        routeType = newRouteType;

        Debug.Log(newRouteType.cardType.ToString());

        routeTypeObj = GameObject.Instantiate(prefabMap[newRouteType.cardType], fromStation.infoPoint.transform, true);
        routeTypeObj.transform.position = fromStation.infoPoint.transform.position;
        var dir = toStation.infoPoint.transform.position - fromStation.infoPoint.transform.position;
        dir.Normalize();
        routeTypeObj.transform.position += dir * 36f;
        routeTypeObj.transform.LookAt(toStation.infoPoint.transform);

        // fix bad model rotations ... . .. .. 
        if (newRouteType.cardType == CardType.Train || newRouteType.cardType == CardType.Plane)
        {
            routeTypeObj.transform.eulerAngles += new Vector3(0f, 90f, 0f);
        }
        else if (newRouteType.cardType == CardType.Rocket)
        {
            routeTypeObj.transform.eulerAngles += new Vector3(90f, 0f, 0f);
            routeTypeObj.transform.localScale = new Vector3(1f / 40f, 1f / 40f, 1f / 9f);
        }
        else if (newRouteType.cardType == CardType.Hyperloop)
        {
            routeTypeObj.transform.eulerAngles += new Vector3(0f, 90f, 90f);
            routeTypeObj.transform.localScale = new Vector3(1f / 9f, 1f / 40f, 1f / 40f);
        }

        routeTypeObj.transform.localScale *= 1.5f;

        var paintBodies = routeTypeObj.GetComponentsInChildren<Renderer>();
        foreach (var rend in paintBodies)
        {
            if (rend.gameObject.name.StartsWith("Body"))
            {
                rend.material.color = player.colour;
            }
        }
    }
}

public class StationInfo
{
    public int id;
    public GameObject obj;
    public Transform infoPoint;
    public Color colour;
    public Dictionary<int, int> peeps;
    public List<Route> routes;

    public StationInfo(int stationId, GameObject stationObj, Color colour)
    {
        this.id = stationId;
        this.obj = stationObj;
        this.colour = colour;
        this.obj.GetComponent<Renderer>().material.color = this.colour;

        infoPoint = stationObj.transform.FindChild("InfoPoint");
        peeps = new Dictionary<int, int>();

        ClearPeeps();
        ClearRouteMap();
    }

    public void ClearPeeps()
    {
        for (int i = 0; i < 8; i++)
        {
            peeps[i] = 0;
        }
    }

    public void ModifyPeeps(int toStation, int amount)
    {
        peeps[toStation] += amount;
        if (peeps[toStation] < 0)
        {
            peeps[toStation] = 0;
        }
    }

    public void AddRoute(Route route)
    {
        routes.Add(route);
    }

    public void ClearRouteMap()
    {
        routes = new List<Route>();
    }
}

public class Player
{
    public int id;
    public NamePlate nameplate;
    public int cash;
    public int peeps;
    public bool alive;
    public GameObject cardPanel;
    public System.Random random;
    public Color colour;

    public Player(int id, NamePlate nameplate, int cash, GameObject cardPanel, System.Random random)
    {
        this.id = id;
        this.cash = cash;
        this.nameplate = nameplate;
        this.peeps = 0;
        this.cardPanel = cardPanel;
        this.random = random;

        alive = true;
        nameplate.SetValues(cash, peeps);

        if (id == 0) { colour = new Color(0f, 0f, 0.5f); }
        if (id == 1) { colour = new Color(0.5f, 0f, 0f); }
        if (id == 2) { colour = new Color(0f, 0.5f, 0f); }
        if (id == 3) { colour = new Color(0.5f, 0f, 0.58f); }
    }

    // remove ui creation code back to the game, probably
    public void DealCards(Game game, Dictionary<CardType, GameObject> cardPrefabMap, GameObject ui)
    {
        for (int i = 0; i < 4; ++i)
        {
            GameObject cardPrefab = GameObject.Instantiate(cardPrefabMap[Card.cardIndexTypeMap[i]], cardPanel.transform);

            var rect = cardPanel.GetComponent<RectTransform>();
            var mid = ui.transform.position.x;            
            
            cardPrefab.transform.position = new Vector3(mid - (156) + (104f * i), 89, 0f);

            var card = cardPrefab.GetComponent<Card>();
            card.playerId = id;
            card.clickHandler = game.CardClickedHandler;
            card.enterHandler = game.CardEnterHandler;
            card.exitHandler = game.CardExitHandler;            
        }
    }
}

public class Game : MonoBehaviour
{
    List<StationInfo> stations;
    Dictionary<int, int> peeps;
    SelectionMode selectionMode = SelectionMode.Card;

    int currentPlayer = 0;
    int humanPlayer = 0;

    public int c_initialCash;

    List<Player> players;
    RouteSelection currentRoute;

    GameObject UI;

    GameObject uiCardList;
    GameObject uiInfoText;
    GameObject uiInstructions;
    GameObject uiPlayerList;
    GameObject uiIntro;
    GameObject uiCancelRoute;
    GameObject uiEndGame;

    Dictionary<int, GameObject> playerHighlightMap;

    Dictionary<CardType, GameObject> cardPrefabMap;
    List<Card> allCards;

    GameObject peepPrefab;
    GameObject stationPrefab;
    Dictionary<CardType, GameObject> routePrefabMap;

    // stationPeepPrefabs[fromStation][toStation] = ?gameObject?
    Dictionary<int, Dictionary<int, Stack<GameObject>>> stationPeepPrefabs;

    // ugly camera things
    Transform cameraOrig;
    int currentStationId;
    float targetCameraAngle;
    float[] cameraStationAngles = { -135f, -90f, -45f, 0f, 45f, 90f, 135f, 180f };
    float defaultCameraAngle = 22.5f;

    Color[] stationColours = {
        new Color(0.5f, 0, 0),
        new Color(0, 0, 0.5f),
        new Color(0.7f, 0.7f, 0),
        Color.black,
        new Color(0, 0.5f, 0),
        new Color(0.1f, 0.7f, 0.7f),
        new Color(0.75f, 0.75f, 0.75f),
        new Color(0.5f, 0, 0.58f),
    };

    Vector3[] stationPositions =
    {
        new Vector3(106.066f, 1f, 106.066f),
        new Vector3(150f, 1f, 0f),
        new Vector3(106.066f, 1f, -106.066f),
        new Vector3(0f, 1f, -150f),
        new Vector3(-106.066f, 1f, -106.066f),
        new Vector3(-150, 1f, 0f),
        new Vector3(-106.066f, 1f, 106.066f),
        new Vector3(0f, 1f, 150f),
    };

    System.Random random = new System.Random();

    // Use this for initialization
    void Start()
    {
        InitStart();
    }

    public void InitStart()
    {
        InitPrefabs();
        InitCamera();
        InitUI();
        ShowIntro();
    }

    void ShowIntro()
    {
        uiCardList.SetActive(false);
        uiInstructions.SetActive(false);
        uiPlayerList.SetActive(false);
    }

    public void EndIntro()
    {
        uiCardList.SetActive(true);
        uiInstructions.SetActive(true);
        uiPlayerList.SetActive(true);

        uiIntro.SetActive(false);
        NewGame();
    }

    void NewGame()
    {
        InitStations();
        InitPeeps();
        InitBoard();
        InitPlayers();

        NewRound();
        NewTurn(true); // initial turn
    }

    void NewRound()
    {
        currentPlayer = 0;
        selectionMode = SelectionMode.Card;
        UpdateInstructions();
        playerHighlightMap[currentPlayer].SetActive(true);
    }

    void EndRound()
    {
        // for every form of transportation a station has,
        // it'll get +X peeps to random destinations each round
        // where X is how many routes * how much they carry
        Dictionary<StationInfo, int> newPeepsPerStation = new Dictionary<StationInfo, int>();
        for (int i = 0; i < 8; i++) { newPeepsPerStation[stations[i]] = 0; }

        // move peeps and tally cash
        foreach (var station in stations)
        {
            // apply every station route
            foreach (var route in station.routes)
            {
                var to = route.toStation.id;
                if (station.peeps.ContainsKey(to))
                {
                    var maxMoved = route.routeType.peepsMoved;
                    var peeps = station.peeps[to];
                    var moved = 0;

                    // don't move more than exist
                    if (peeps != 0)
                    {
                        if (peeps < maxMoved)
                        {
                            moved = peeps;
                        }
                        else
                        {
                            moved = maxMoved;
                        }

                        ModifyPeeps(station, route.toStation, -moved);
                        route.player.cash += 2 * moved;
                        route.player.peeps += moved;
                        newPeepsPerStation[station] += 1;
                    }
                }
            }
        }
        UpdateNameplates();

        // add new peeps, each station gets 2 per turn at random
        AddPeepToAllStations();
        AddPeepToAllStations();

        // add peeps with random destination
        // based on bonus peep generation formula above
        foreach (var item in newPeepsPerStation)
        {
            for (int i = 0; i < item.Value; ++i)
            {
                ModifyPeeps(item.Key, GetRandomDestinationStation(item.Key.id), 1);
            }
        }
        
        NewRound();
    }

    void GameOver()
    {
        selectionMode = SelectionMode.GameOver;
        uiCardList.SetActive(false);
        uiInstructions.SetActive(false);
        uiInfoText.SetActive(false);
        uiEndGame.SetActive(true);

        var text = uiEndGame.GetComponentInChildren<UnityEngine.UI.Text>();

        players.Sort((a, b) => b.peeps - a.peeps);

        text.text = "<b>Player " + (players[0].id + 1) + " Wins!</b>\n<size=38>\n";
        text.text += "#1 Player " + (players[0].id + 1) + ": " + players[0].peeps + " peeps moved\n";
        text.text += "#2 Player " + (players[1].id + 1) + ": " + players[1].peeps + " peeps moved\n";
        text.text += "#3 Player " + (players[2].id + 1) + ": " + players[2].peeps + " peeps moved\n";
        text.text += "#4 Player " + (players[3].id + 1) + ": " + players[3].peeps + " peeps moved\n";
        text.text += "\n\n\n\n\n";
        text.text += "</size><size=24>there's no way to restart the game\n";
        text.text += "it's okay, you can quit, I won't feel bad";
        text.text += "</size>";
    }

    void NewTurn(bool initial)
    {
        UpdateNameplates();

        // represents how many stations are fully routed
        // if counter == 8 the game is over
        int endGameCounter = 0;
        foreach (var station in stations)
        {
            if (station.routes.Count == 7)
            {
                endGameCounter++;
            }
        }

        // check end condition (all routes complete)        
        if (endGameCounter == 8)
        {
            GameOver();
            Debug.Log("game over!");
            return;
        }
        
        playerHighlightMap[currentPlayer].SetActive(false);

        if (!initial)
        {
            currentPlayer++;
            if (currentPlayer > 3)
            {
                EndRound();
                if (selectionMode == SelectionMode.GameOver)
                    return;
            }
        }

        playerHighlightMap[currentPlayer].SetActive(true);

        if (currentPlayer == humanPlayer)
        {
            HumanTurn();
        }
        else
        {
            AiTurn();
        }
    }

    void HumanTurn()
    {
        uiCardList.SetActive(true);
        uiInstructions.SetActive(true);
        selectionMode = SelectionMode.Card;

        var player = players[currentPlayer];
        foreach (var card in allCards)
        {
            var img = card.GetComponent<UnityEngine.UI.Image>();
            if (card.routeCost > player.cash)
            {
                img.color = new Color(0.3f, 0.3f, 0.3f);
            }
            else
            {
                img.color = new Color(1f, 1f, 1f);
            }
        }
    }

    void AiTurn()
    {
        uiCardList.SetActive(false);
        uiInstructions.SetActive(false);
        selectionMode = SelectionMode.CPUMove;

        var player = players[currentPlayer];
        int cash = player.cash;
        CardType routeType = CardType.Train;

        if (cash < 5)
        {
            PassTurn();
            return;
        }

        if (cash >= 50)
        {
            routeType = CardType.Hyperloop;
        }
        else if (cash >= 25)
        {
            routeType = CardType.Rocket;
        }
        else if (cash >= 10)
        {
            routeType = CardType.Plane;
        }

        bool found = false;
        int bestPeeps = int.MinValue;
        RouteSelection bestRoute = new RouteSelection();
        bestRoute.player = player;

        foreach (var fromStation in stations)
        {
            foreach (var toStation in stations)
            {
                if (fromStation != toStation && !RouteExists(fromStation, toStation))
                {
                    if (fromStation.peeps[toStation.id] > bestPeeps)
                    {
                        bestRoute.fromStation = fromStation;
                        bestRoute.toStation = toStation;
                        found = true;
                    }
                }
            }
        }

        if (found)
        {
            foreach (var card in allCards)
            {
                if (card.cardType == routeType)
                {
                    bestRoute.routeType = card;
                }
            }

            CommitRoute(bestRoute);
        }
        else
        {
            PassTurn();
        }
    }

    public void PassTurn()
    {
        NewTurn(false);
    }

    void UpdateInstructions()
    {
        var info = "";
        if (selectionMode == SelectionMode.Card)
        {
            info = "<size=55><b><color=red>Card Selection</color></b></size>\n";
            info += "Each card costs cash to use\n";
            info += "You may use <color=yellow>one</color> card per turn";
        }
        else if (selectionMode == SelectionMode.RouteFromStation)
        {
            info = "<size=55><b><color=red>Pick Route Start Station</color></b></size>\n";
            info += "Pick your routes starting station\n";
            info += "";
        }
        else if (selectionMode == SelectionMode.RouteToStation)
        {
            info = "<size=55><b><color=red>Pick Route End Station</color></b></size>\n";
            info += "Pick your routes ending station\n";
            info += "Remember each card has a maximum range";
        }

        var text = uiInstructions.GetComponentInChildren<UnityEngine.UI.Text>();
        if (text != null)
            text.text = info;

        uiInstructions.SetActive(true);
    }

    void UpdateNameplates()
    {
        foreach (var player in players)
        {
            player.nameplate.gameObject.SetActive(true);
            player.nameplate.SetValues(player.cash, player.peeps);
        }
    }

    void InitCards()
    {
        allCards = new List<Card>(uiCardList.GetComponentsInChildren<Card>());
    }

    void InitPrefabs()
    {
        peepPrefab = Resources.Load("Prefabs/Peep") as GameObject;
        stationPrefab = Resources.Load("Prefabs/Station") as GameObject;

        routePrefabMap = new Dictionary<CardType, GameObject>();
        routePrefabMap[CardType.Train] = Resources.Load("Prefabs/Train") as GameObject;
        routePrefabMap[CardType.Plane] = Resources.Load("Prefabs/Plane") as GameObject;
        routePrefabMap[CardType.Rocket] = Resources.Load("Prefabs/Rocket") as GameObject;
        routePrefabMap[CardType.Hyperloop] = Resources.Load("Prefabs/Hyperloop") as GameObject;

        cardPrefabMap = new Dictionary<CardType, GameObject>();
        cardPrefabMap[CardType.Train] = Resources.Load("Prefabs/CardTrain") as GameObject;
        cardPrefabMap[CardType.Plane] = Resources.Load("Prefabs/CardPlane") as GameObject;
        cardPrefabMap[CardType.Rocket] = Resources.Load("Prefabs/CardRocket") as GameObject;
        cardPrefabMap[CardType.Hyperloop] = Resources.Load("Prefabs/CardHyperloop") as GameObject;
    }

    void InitCamera()
    {
        cameraOrig = GameObject.Find("CameraOrigin").transform;
        targetCameraAngle = defaultCameraAngle;
    }

    void InitUI()
    {
        UI = GameObject.Find("UI");
        uiCardList = GameObject.Find("CardList");
        uiInfoText = GameObject.Find("InfoTextPanel");
        uiInstructions = GameObject.Find("InstructionText");
        uiPlayerList = GameObject.Find("PlayerList");
        uiIntro = GameObject.Find("IntroTextPanel");
        uiCancelRoute = GameObject.Find("RouteCancelButton");
        uiEndGame = GameObject.Find("EndGamePanel");

        playerHighlightMap = new Dictionary<int, GameObject>();
        playerHighlightMap[0] = GameObject.Find("Player1Turn");
        playerHighlightMap[1] = GameObject.Find("Player2Turn");
        playerHighlightMap[2] = GameObject.Find("Player3Turn");
        playerHighlightMap[3] = GameObject.Find("Player4Turn");

        playerHighlightMap[0].SetActive(false);
        playerHighlightMap[1].SetActive(false);
        playerHighlightMap[2].SetActive(false);
        playerHighlightMap[3].SetActive(false);

        uiInfoText.SetActive(false);
        uiCancelRoute.SetActive(false);
        uiEndGame.SetActive(false);
    }


    void InitStations()
    {
        GameObject world = GameObject.Find("Stations");

        for (int i = 0; i < 8; ++i)
        {
            GameObject station = GameObject.Instantiate(stationPrefab, stationPositions[i], Quaternion.identity, world.transform);
            station.name = "Station" + (i + 1);
            var inputHandler = station.GetComponent<ClickableObject>();
            inputHandler.downHandler = StationOnMouseDownHandler;
            inputHandler.enterHandler = StationOnMouseEnterHandler;
            inputHandler.exitHandler = StationOnMouseExitHandler;
        }

        stations = new List<StationInfo> {
            new StationInfo(0, GameObject.Find("Station1"), stationColours[0]),
            new StationInfo(1, GameObject.Find("Station2"), stationColours[1]),
            new StationInfo(2, GameObject.Find("Station3"), stationColours[2]),
            new StationInfo(3, GameObject.Find("Station4"), stationColours[3]),
            new StationInfo(4, GameObject.Find("Station5"), stationColours[4]),
            new StationInfo(5, GameObject.Find("Station6"), stationColours[5]),
            new StationInfo(6, GameObject.Find("Station7"), stationColours[6]),
            new StationInfo(7, GameObject.Find("Station8"), stationColours[7])
        };
    }

    void InitPeeps()
    {
        peeps = new Dictionary<int, int>();
        stationPeepPrefabs = new Dictionary<int, Dictionary<int, Stack<GameObject>>>();

        // init subdictionary
        for (int i = 0; i < 8; ++i)
        {
            stationPeepPrefabs[i] = new Dictionary<int, Stack<GameObject>>();
            for (int j = 0; j < 8; ++j)
            {
                if (i != j)
                {
                    stationPeepPrefabs[i][j] = new Stack<GameObject>();
                }
            }
        }
    }

    void InitBoard()
    {
        // add a peep to each station
        for (int i = 0; i < 8; i++)
        {
            AddPeepToAllStations();
        }
    }

    void InitPlayers()
    {
        players = new List<Player>();

        var nameplates = UI.GetComponentsInChildren<NamePlate>(true);

        // get nameplates in player order, just in case they're not ordered
        for (int i = 0; i < 4; ++i)
        {
            for (int j = 0; j < 4; ++j)
            {
                if (nameplates[j].playerId == i)
                {
                    players.Add(new Player(i, nameplates[j], c_initialCash, uiCardList, random));

                    // deal cards to player 1 ui
                    if (i == 0)
                    {
                        players[i].DealCards(this, cardPrefabMap, UI);
                        InitCards();
                    }
                }
            }
        }
    }

    StationInfo GetRandomDestinationStation(int fromStation)
    {
        var stationIds = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7 };
        // remove own key so we don't random stationX->stationX
        stationIds.Remove(fromStation);

        return stations[stationIds[random.Next(0, 7)]];
    }

    // adds a single peep to each station with random destination
    void AddPeepToAllStations()
    {
        for (int i = 0; i < 8; ++i)
        {
            var toStation = GetRandomDestinationStation(i);
            ModifyPeeps(stations[i], toStation, 1);
        }
    }

    void ModifyPeeps(StationInfo fromStation, StationInfo toStation, int amount)
    {
        // cap at 6 peeps per station
        if (fromStation.peeps[toStation.id] + amount > 6)
        {
            amount = 6 - fromStation.peeps[toStation.id];
        }

        UpdatePeepsDisplay(fromStation, toStation, amount);
        stations[fromStation.id].ModifyPeeps(toStation.id, amount);
    }

    void UpdatePeepsDisplay(StationInfo fromStation, StationInfo toStation, int amount)
    {
        int idFrom = fromStation.id;
        int idTo = toStation.id;

        if (amount == 0)
        {
            return;
        }
        else if (amount < 0)
        {
            Debug.Log("removing peep");
            for (int i = amount; i < 0; i++)
            {
                if (stationPeepPrefabs[idFrom][idTo].Count > 0)
                {
                    Debug.Log("destroying peep prefab");
                    DestroyImmediate(stationPeepPrefabs[idFrom][idTo].Pop());
                }
                else
                {
                    Debug.Log("tried to remove more peeps than exist");
                }
            }
        }
        else
        {
            var go = GameObject.Instantiate(peepPrefab, fromStation.infoPoint);
            SetPeepColour(go, stationColours[idTo]);
            stationPeepPrefabs[idFrom][idTo].Push(go);

            // move peep to edge on a point closest to destination station
            var moveAxis = (stations[idTo].obj.transform.position -
                            stations[idFrom].obj.transform.position);
            moveAxis.Normalize();
            moveAxis.y = -0.4f + (0.37f * stationPeepPrefabs[idFrom][idTo].Count);
            go.transform.position += moveAxis * 16f;
        }
    }

    void SetPeepColour(GameObject peep, Color colour)
    {
        peep.transform.FindChild("Cube").GetComponent<Renderer>().material.color = colour;
        peep.transform.FindChild("Sphere").GetComponent<Renderer>().material.color = colour;
    }

    void SwingCameraToStation(StationInfo toStation)
    {
        targetCameraAngle = cameraStationAngles[toStation.id];
        Debug.Log("turning camera " + targetCameraAngle + " degrees");
        cameraOrig.rotation = Quaternion.Euler(0f, targetCameraAngle, 0f);
    }

    void StationOnMouseDownHandler(GameObject stationObj)
    {
        // find matching station
        for (int i = 0; i < 8; i++)
        {
            if (stations[i].obj == stationObj)
            {
                if (selectionMode == SelectionMode.Card)
                {
                    SwingCameraToStation(stations[i]);
                    break;
                }
                else if (selectionMode == SelectionMode.RouteFromStation)
                {
                    currentRoute.fromStation = stations[i];
                    //SwingCameraToStation(stations[i]); // too jarring without camera lerp
                    StartEndStationSelection();
                    break;
                }
                else if (selectionMode == SelectionMode.RouteToStation)
                {
                    if (!RouteExists(currentRoute.fromStation, stations[i]))
                    {
                        currentRoute.toStation = stations[i];
                        CommitRoute(currentRoute);
                    }
                    break;
                }
            }
        }
    }

    void StationOnMouseEnterHandler(GameObject stationObj)
    {
        stationObj.GetComponent<Renderer>().material.color += new Color(0.25f, 0.25f, 0.25f);
    }

    void StationOnMouseExitHandler(GameObject stationObj)
    {
        stationObj.GetComponent<Renderer>().material.color -= new Color(0.25f, 0.25f, 0.25f);
    }

    public void CardClickedHandler(Card card)
    {
        if (card.routeCost <= players[currentPlayer].cash)
        {
            CardExitHandler(card);
            StartRouteSelection(card);
        }
        else
        {
            Debug.Log("can't afford action");
        }
    }

    public void CardEnterHandler(Card card)
    {
        uiInfoText.SetActive(true);
        card.PlaceText(uiInfoText);
    }

    public void CardExitHandler(Card card)
    {
        uiInfoText.SetActive(false);
    }

    public void CancelRouteSelection()
    {
        selectionMode = SelectionMode.Card;
        uiCancelRoute.SetActive(false);
        uiCardList.SetActive(true);
        UpdateInstructions();
    }

    void StartRouteSelection(Card card)
    {
        selectionMode = SelectionMode.RouteFromStation;

        currentRoute = new RouteSelection();
        currentRoute.routeType = card;
        currentRoute.player = players[currentPlayer];

        uiCancelRoute.SetActive(true);
        uiCardList.SetActive(false);
        UpdateInstructions();
    }

    void StartEndStationSelection()
    {
        selectionMode = SelectionMode.RouteToStation;

        UpdateInstructions();
    }

    void CommitRoute(RouteSelection route)
    {
        uiCancelRoute.SetActive(false);
        route.fromStation.AddRoute(new Route(route, routePrefabMap));
        players[currentPlayer].cash -= route.routeType.routeCost;
        NewTurn(false);
    }

    bool RouteExists(StationInfo from, StationInfo to)
    {
        foreach (var route in from.routes)
        {
            if (route.toStation == to)
            {
                return true;
            }
        }

        return false;
    }

    /*
    void UpdateCameraSwing()
    {
        // broken, fix if I have time to come back to it
        float camAngle = cameraOrig.rotation.eulerAngles.y;
        float camDir = Mathf.Sign(targetStationAngle);

        float first = Mathf.Max(camAngle, targetStationAngle);
        float second = Mathf.Min(targetStationAngle, camAngle);

        if (targetStationAngle - Mathf.Epsilon <= 0)
        {
            float delta = 50f * Time.deltaTime * camDir;

            if (camDir > 0f)
            {
                // fix camera over-swinging view
                if (targetStationAngle - delta < 0f)
                    delta = targetStationAngle;
            }
            else
            {
                // fix camera over-swinging view
                if (targetStationAngle - delta > 0f)
                    delta = targetStationAngle;
            }

            targetStationAngle -= delta;
            cameraOrig.Rotate(new Vector3(0f, delta, 0f));
        }
    }
    */

    void UpdateStationHighlights()
    {
        Ray ray;
        RaycastHit hit;

        ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit))
        {
            //Left Click, change to red.
            if (Input.GetMouseButtonDown(0))
            {
                Debug.Log(hit.collider.gameObject.name);
            }
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) { SwingCameraToStation(stations[0]); }
        if (Input.GetKeyDown(KeyCode.Alpha2)) { SwingCameraToStation(stations[1]); }
        if (Input.GetKeyDown(KeyCode.Alpha3)) { SwingCameraToStation(stations[2]); }
        if (Input.GetKeyDown(KeyCode.Alpha4)) { SwingCameraToStation(stations[3]); }
        if (Input.GetKeyDown(KeyCode.Alpha5)) { SwingCameraToStation(stations[4]); }
        if (Input.GetKeyDown(KeyCode.Alpha6)) { SwingCameraToStation(stations[5]); }
        if (Input.GetKeyDown(KeyCode.Alpha7)) { SwingCameraToStation(stations[6]); }
        if (Input.GetKeyDown(KeyCode.Alpha8)) { SwingCameraToStation(stations[7]); }
    }
}
