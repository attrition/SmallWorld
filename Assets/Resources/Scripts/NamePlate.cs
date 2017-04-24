using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NamePlate : MonoBehaviour
{
    public int playerId;
    public Text cashText;
    public Text peepsText;

    // Use this for initialization
    void Start()
    {
    }

    public void SetValues(int cash, int peeps)
    {
        cashText.text = "$" + cash;
        peepsText.text = peeps.ToString();
    }
    
    // Update is called once per frame
    void Update()
    {
    }
}
