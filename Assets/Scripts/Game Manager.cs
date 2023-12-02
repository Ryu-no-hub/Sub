using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    private GameObject UIPanel;
    private Dictionary<string, int> currentIconCounts;
    // Start is called before the first frame update
    void Start()
    {
        UIPanel = GameObject.Find("Panel");
        UIPanel.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetIcons(Dictionary<string, int> iconCounts)
    {
        currentIconCounts = iconCounts;

        ResetPanel();

        RectTransform panelRect = UIPanel.GetComponent<RectTransform>();
        int X = (int)panelRect.position.x - (int)panelRect.rect.width/2 + 50;
        foreach(KeyValuePair<string, int> nameCount in currentIconCounts)
        {
            string name = nameCount.Key;
            int count = nameCount.Value;
            Vector3 iconPos;

            GameObject icon = UIPanel.transform.Find(name + "_icon").gameObject;
            icon.transform.Find("Count").gameObject.GetComponent<TextMeshProUGUI>().text = count.ToString();
            iconPos = icon.GetComponent<RectTransform>().position;
            iconPos.x = X;
            X += 70;
            icon.GetComponent<RectTransform>().position = iconPos;

            icon.SetActive(true);
        }
    }

    public void DeactivatePanel()
    {
        UIPanel.SetActive(false);
    }

    private void ResetPanel()
    {
        // Turn off all previously enabled icons
        foreach (Transform child in UIPanel.transform)
            child.gameObject.SetActive(false);
        if (!UIPanel.activeInHierarchy)
            UIPanel.SetActive(true);
    }

    public void BuildPanel()
    {
        ResetPanel();

        RectTransform panelRect = UIPanel.GetComponent<RectTransform>();
        int X = (int)panelRect.position.x - (int)panelRect.rect.width / 2 + 72;
        foreach (Transform child in UIPanel.transform)
        {
            name = child.name;
            Vector3 iconPos;
            print("name = " + name);
            //if (name.IndexOf('_') == -1) continue;
            if (name.Substring(0, 1) == "B")
            {
                iconPos = child.GetComponent<RectTransform>().position;
                iconPos.x = X;
                X += 72 + 20;
                child.GetComponent<RectTransform>().position = iconPos;

                child.gameObject.SetActive(true);
            }

        }
    }

    public void Build_one()
    {
        Build(1);
    }
    public void Build_two()
    {
        Build(2);
    }
    public void Build_three()
    {
        Build(3);
    }
    public void Build_four()
    {
        Build(4);
    }
    public void Build_five()
    {
        Build(5);
    }

    private void Build(int builIndex)
    {
        print(builIndex);
    }
}
