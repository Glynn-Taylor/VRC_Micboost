
using System;
using System.Collections.Generic;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class SpoonieMicBoost : UdonSharpBehaviour
{
    [SerializeField] RectTransform UIListRoot;
    [SerializeField] TMPro.TextMeshProUGUI boostedPeopleLabel;
    [SerializeField] Animator PanelAnimator;

    [SerializeField] UnityEngine.UI.Image boostGainStatus;
    [SerializeField] UnityEngine.UI.Image volumetricRadiusStatus;
    [SerializeField] UnityEngine.UI.Image lowpassStatus;
    [SerializeField] TMPro.TextMeshProUGUI boostedGainLabel;

    [SerializeField] TMPro.TextMeshProUGUI sliderNearLabel;
    [SerializeField] Slider sliderNearSlider;
    [SerializeField] TMPro.TextMeshProUGUI sliderFarLabel;
    [SerializeField] Slider sliderFarSlider;

    //Defaults (https://creators.vrchat.com/worlds/udon/players/player-audio/)
    //Gain = 15 (0-24)
    //Near = 0 (0-1,000,000)            Start of falloff
    //Far = 25 (0-1,000,000)            End of falloff (completely silent)
    //Volumetric radius = 0 (0-1000)    Make sounds sound close even when distant, keep it lower than distance far
    //Voice lowpass = On (On/Off)       Normally distant players are passed through a lowpass filter to reduce audio, turn off for performances

    // Syncable variables https://udonsharp.docs.vrchat.com/vrchat-api/#synced-variables
    [UdonSynced] string boostedPlayers = "";
    [UdonSynced] bool boostGain = false;
    [UdonSynced] bool volumetricRadius = false;
    [UdonSynced] bool lowpass = false;
    [UdonSynced] int boostedGain = 0;
    [UdonSynced] int boostedNearDistance = 125;
    [UdonSynced] int boostedFarDistance = 125;

    public void Toggle()
    {
        if (PanelAnimator != null)
        {
            PanelAnimator.SetBool("Show", !PanelAnimator.GetBool("Show"));
        }
    }

    //___________________________________________________________________________________________________________________
    //                                                  BUTTON CLICK
    //___________________________________________________________________________________________________________________
    public void OnButtonClick()
    {
        Debug.Log("[SpoonieMicBoost] UI Button Clicked");
        if (UIListRoot != null)
        {
            for (int i = 0; i < UIListRoot.childCount; i++)
            {
                Transform button = UIListRoot.GetChild(i);
                if (button.gameObject.activeSelf)
                {
                    //Check trigger 
                    if (!button.GetChild(1).GetComponent<UnityEngine.UI.Image>().enabled)
                    {
                        string pName = button.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text;
                        //Turn On
                        if (!BoostedPlayersContains(pName))
                        {
                            //Names can't be longer than 15 characters
                            //Names have to conform to unicode spec no windings
                            //Characters like | or , can't be used, can check on the site
                            if (boostedPlayers.Length + pName.Length < 129)  //Capped for network dumbshittery
                            {
                                Debug.Log("[SpoonieMicBoost] Boosting " + pName);
                                if (!Networking.IsOwner(gameObject))
                                {
                                    Networking.SetOwner(Networking.LocalPlayer, gameObject);
                                }
                                //Add seperator if required
                                if (boostedPlayers.Length > 0)
                                {
                                    boostedPlayers += "|";
                                }
                                boostedPlayers += pName;
                                RequestSerialization();
                                OnDeserialization();
                            }
                            else
                            {
                                if (PanelAnimator != null)
                                {
                                    PanelAnimator.SetTrigger("WarningSizeLimit");
                                }
                            }
                        }
                        //Turn Off
                        else
                        {
                            Debug.Log("[SpoonieMicBoost] Unboosting " + pName);
                            if (!Networking.IsOwner(gameObject))
                            {
                                Networking.SetOwner(Networking.LocalPlayer, gameObject);
                            }
                            RemoveName(pName);

                        }
                        //reset trigger
                        button.GetChild(1).GetComponent<UnityEngine.UI.Image>().enabled = true;
                    }
                }
            }
        }
    }

    void RemoveName(string pName)
    {
        int index = boostedPlayers.IndexOf(pName);
        if (index >= 0)
        {
            if (index > 0) //Remove with seperator
            {
                boostedPlayers = boostedPlayers.Remove(index - 1, pName.Length + 1);
            }
            else
            {
                boostedPlayers = boostedPlayers.Remove(index, pName.Length);
                if (boostedPlayers.StartsWith("|")) //Remove divider prefix of what was the second element but is now first
                {
                    boostedPlayers = boostedPlayers.Remove(0, 1);
                }
            }
            RequestSerialization();
            OnDeserialization();
        }
    }

    public void ToggleBoostGain()
    {
        if (!Networking.IsOwner(gameObject))
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }
        boostGain = !boostGain;
        RequestSerialization();
        OnDeserialization();
    }

    public void ToggleLowPass()
    {
        if (!Networking.IsOwner(gameObject))
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }
        lowpass = !lowpass;
        RequestSerialization();
        OnDeserialization();
    }

    public void ToggleVolumetricRadius()
    {
        if (!Networking.IsOwner(gameObject))
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }
        volumetricRadius = !volumetricRadius;
        RequestSerialization();
        OnDeserialization();
    }

    public void BoostedGainIncrease()
    {
        if (!Networking.IsOwner(gameObject))
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }
        boostedGain = Math.Min(24, boostedGain + 2);
        RequestSerialization();
        OnDeserialization();
    }

    public void BoostedGainDecrease()
    {
        if (!Networking.IsOwner(gameObject))
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }
        boostedGain = Math.Max(0, boostedGain - 2);
        RequestSerialization();
        OnDeserialization();
    }

    public void UpdateFarDistance()
    {
        int val = Mathf.RoundToInt(sliderFarSlider.value);
        if (val == boostedFarDistance)
            return;
        if (!Networking.IsOwner(gameObject))
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }
        boostedFarDistance = val;

        CheckNearLessThanFar(true);
        RequestSerialization();
        OnDeserialization();
    }

    private void CheckNearLessThanFar(bool adjustNear)
    {
        if (boostedNearDistance > boostedFarDistance)
        {
            if (adjustNear)
            {
                boostedNearDistance = boostedFarDistance;
            }
            else
            {
                boostedFarDistance = boostedNearDistance;
            }
        }
    }

    public void UpdateNearDistance()
    {
        int val = Mathf.RoundToInt(sliderNearSlider.value);
        if (val == boostedNearDistance)
            return;
        if (!Networking.IsOwner(gameObject))
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }
        boostedNearDistance = val;

        CheckNearLessThanFar(false);
        RequestSerialization();
        OnDeserialization();
    }

    public void TestSliderEnd()
    {
        Debug.Log("Finished Drag");
    }

    //___________________________________________________________________________________________________________________
    //                                                  GENERATION
    //___________________________________________________________________________________________________________________

    //https://www.calhoun.io/lets-learn-algorithms-sorting-a-list-of-strings-in-alphabetical-order-with-bubble-sort/
    /*void BubbleSort(VRCPlayerApi[] playerArray)
    {
        //int N = playerArray.Length;
        for (int i = 0; i < playerArray.Length; i++)
        {
            //if (playerArray[i] != null && playerArray[i].IsValid())
            //{
            int firstIndex = 0;
            for (int secondIndex = 1; i < playerArray.Length; secondIndex++)
            {
                if ((playerArray[firstIndex] != null && playerArray[firstIndex].IsValid()) && (playerArray[secondIndex] != null && playerArray[secondIndex].IsValid()))
                {
                    if (playerArray[firstIndex].displayName[0] > playerArray[secondIndex].displayName[0])
                    {
                        VRCPlayerApi firstPlayer = playerArray[firstIndex];
                        playerArray[firstIndex] = playerArray[secondIndex];
                        playerArray[secondIndex] = firstPlayer;
                    }
                }
                else
                {
                    VRCPlayerApi firstPlayer = playerArray[firstIndex];
                    playerArray[firstIndex] = playerArray[secondIndex];
                    playerArray[secondIndex] = firstPlayer;
                }

                firstIndex++;
            }
            //}
        }
    }*/

    string[] GenerateCleanPlayerlist(VRCPlayerApi[] playerArray)
    {
        //Man I wish linq was a thing;
        int cleanPlayers = 0;
        //First pass
        for (int i = 0; i < playerArray.Length; i++)
        {
            if (playerArray[i] != null && playerArray[i].IsValid() && playerArray[i].displayName.Length > 0)
            {
                cleanPlayers++;
            }
        }
        //Second pass
        string[] playerNames = new string[cleanPlayers];
        int nameIndex = 0;
        for (int i = 0; i < playerArray.Length; i++)
        {
            if (playerArray[i] != null && playerArray[i].IsValid() && playerArray[i].displayName.Length > 0)
            {
                playerNames[nameIndex] = playerArray[i].displayName;
                nameIndex++;
            }
        }
        return playerNames;
    }

    //Input must contain no empty strings, null or invalid players
    string[] BubbleSort(string[] playerArray)
    {
        if (playerArray.Length < 2)
            return playerArray;

        var n = playerArray.Length;

        for (int i = 0; i < n - 1; i++)
            for (int j = 0; j < n - i - 1; j++)
                if (playerArray[j][0] > playerArray[j + 1][0])
                {
                    string tempVar = playerArray[j];
                    playerArray[j] = playerArray[j + 1];
                    playerArray[j + 1] = tempVar;
                }

        return playerArray;
    }

    // ListRoot
    //  --> Button
    //      --> Label
    //      --> Selected
    //VRCPlayerApi[] players = new VRCPlayerApi[80]; //HAVE TO DO IT THIS WAY AS VRCHAT ERRORS ON LOCAL
    private void GenerateUIList()
    {
        VRCPlayerApi[] players2 = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()]; //Check to see if this stops dupes
        players2 = VRCPlayerApi.GetPlayers(players2);
        string[] sortedPlayers = GenerateCleanPlayerlist(players2);
        sortedPlayers = BubbleSort(sortedPlayers);
        /************* Sort not supported
        string[] sortedPlayers = new string[players2.Length];
        for (int i = 0; i < players2.Length; i++)
        {
            sortedPlayers[i] = players2[i].displayName;
        }
        Array.Sort(sortedPlayers);
        */
        /************* List not supported
        List<string> sortedPlayers = new List<string>();
        for (int i = 0; i < players2.Length; i++)
        {
            if (players2[i] != null && players2[i].IsValid())
                sortedPlayers.Add(players2[i].displayName);
        }
        sortedPlayers.Sort();
        */
        //VRCPlayerApi.GetPlayers(players2);

        if (UIListRoot != null)
        {
            for (int i = 0; i < UIListRoot.childCount; i++)
            {
                if (i < sortedPlayers.Length)
                {

                    Transform button = UIListRoot.GetChild(i);
                    button.gameObject.SetActive(true);
                    //Set text
                    button.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = sortedPlayers[i];
                    //Selected
                    if (BoostedPlayersContains(sortedPlayers[i]))
                    {
                        button.GetChild(1).gameObject.SetActive(true);
                    }
                    else
                    {
                        button.GetChild(1).gameObject.SetActive(false);
                    }

                }
                else
                {
                    UIListRoot.GetChild(i).gameObject.SetActive(false);
                }
            }
        }
        /*
        if (UIListRoot != null)
        {
            for (int i = 0; i < UIListRoot.childCount; i++)
            {
                if (i < players2.Length)
                {
                    if (players2[i] != null && players2[i].IsValid()) //Check if player hasn't left
                    {
                        Transform button = UIListRoot.GetChild(i);
                        button.gameObject.SetActive(true);
                        //Set text
                        button.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = players2[i].displayName;
                        //Selected
                        if (BoostedPlayersContains(players2[i].displayName))
                        {
                            button.GetChild(1).gameObject.SetActive(true);
                        }
                        else
                        {
                            button.GetChild(1).gameObject.SetActive(false);
                        }
                    }
                    else
                    {
                        UIListRoot.GetChild(i).gameObject.SetActive(false);
                    }
                }
                else
                {
                    UIListRoot.GetChild(i).gameObject.SetActive(false);
                }
            }
        }*/
    }

    private bool BoostedPlayersContains(string displayName)
    {
        //do precheck to save a small amount of time in case of no hit, .Contains is faster than manual iteration
        return (boostedPlayers.Contains(displayName) && DuplicateSubnameCheck(displayName));
    }

    private bool DuplicateSubnameCheck(string displayName)
    {
        string[] names = boostedPlayers.Split('|');
        for (int i = 0; i < names.Length; i++)
        {
            if (names[i].Equals(displayName))
            {
                return true;
            }
        }
        return false;
    }

    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        GenerateUIList(); //Add them to list
    }

    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        if (Networking.IsOwner(gameObject))
        {
            if (BoostedPlayersContains(player.displayName)) //Check what happens if master leaves maybe
            {
                RemoveName(player.displayName);
            }
        }
    }



    private void Start()
    {
        //GenerateUIList();
        OnDeserialization();
    }


    //___________________________________________________________________________________________________________________
    //                                                  SYNC
    //___________________________________________________________________________________________________________________
    public override void OnDeserialization()
    {
        Debug.Log("[SpoonieMicBoost] Deserializing");
        AdjustPlayerVoices();
        GenerateUIList();
        if (boostedPeopleLabel != null)
        {
            boostedPeopleLabel.text = "Boosted: " + boostedPlayers;
        }
        if (boostGainStatus != null)
            boostGainStatus.color = boostGain ? Color.green : Color.red;
        if (volumetricRadiusStatus != null)
            volumetricRadiusStatus.color = volumetricRadius ? Color.green : Color.red;
        if (lowpassStatus != null)
            lowpassStatus.color = lowpass ? Color.green : Color.red;
        if (boostedGainLabel != null)
            boostedGainLabel.text = "Gain: " + boostedGain.ToString();

        sliderFarLabel.text = boostedFarDistance.ToString();
        sliderFarSlider.SetValueWithoutNotify(boostedFarDistance);

        sliderNearLabel.text = boostedNearDistance.ToString();
        sliderNearSlider.SetValueWithoutNotify(boostedNearDistance);
    }

    public override void OnPostSerialization(SerializationResult result)
    {
        if (result.success)
        {
            Debug.Log("[SpoonieMicBoost] Send success");
            if (PanelAnimator != null)
            {
                PanelAnimator.SetTrigger("WarningSendSuccess");
            }
        }
        else
        {
            Debug.Log("[SpoonieMicBoost] Send failure");
            if (PanelAnimator != null)
            {
                PanelAnimator.SetTrigger("WarningSendFail");
            }
        }
    }


    //near less equal to far

    //Event triggers
    //  - List change
    //  - Global toggle off/on
    public void AdjustPlayerVoices()
    {
        VRCPlayerApi[] players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()]; //Check to see if this stops dupes
        players = VRCPlayerApi.GetPlayers(players);
        //VRCPlayerApi.GetPlayers(players);
        bool boostPlayer = false;

        if (players != null)
        {
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] != null && players[i].IsValid())
                {
                    boostPlayer = BoostedPlayersContains(players[i].displayName);

                    players[i].SetVoiceGain(boostPlayer ? boostedGain : 15);
                    players[i].SetVoiceDistanceNear(boostPlayer ? boostedNearDistance : 0);
                    players[i].SetVoiceDistanceFar(boostPlayer ? boostedFarDistance : 25);
                    players[i].SetVoiceVolumetricRadius((boostPlayer && volumetricRadius) ? 125 : 0);
                    players[i].SetVoiceLowpass((boostPlayer && lowpass) ? false : true);
                }
            }
        }
    }
}
