using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BasicRBGUI 
{
    public static void DisplayDataNextToRB(string text, int fontSize, Vector3 rbPos)
    {
        GUIStyle textStyle = new GUIStyle();

        textStyle.fontSize = fontSize;
        textStyle.normal.textColor = UnityEngine.Color.black;

        //The position and size of the text area
        Rect textArea = new Rect(10, 10, 200, fontSize);

        //From world space to screen space
        Vector3 screenPos = Camera.main.WorldToScreenPoint(rbPos);

        //WorldToScreenPoint and GUI.Label y positions are for some reason inverted
        screenPos.y = Screen.height - screenPos.y;

        //We also want it centered
        screenPos.y -= textArea.height * 0.5f;

        //And offset it in x direction so its outside of the object
        screenPos.x += 30f;

        textArea.position = new Vector2(screenPos.x, screenPos.y);

        GUI.Label(textArea, text, textStyle);
    }
}
