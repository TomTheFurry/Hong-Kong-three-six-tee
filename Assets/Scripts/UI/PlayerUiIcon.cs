using cakeslice;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerUiIcon : MonoBehaviour
{
    public static bool IsHovered = false;
    public static PlayerUiIcon PlayerUiIconHovered = null;

    public static List<PlayerUiIcon> Instances = new List<PlayerUiIcon>();

    private Action callBack = null;
    private TMPro.TMP_Text _Text;
    public TMPro.TMP_Text Text
    {
        set
        {
            _Text = value;
            _Text.color = Color.green;
        }
        get
        {
            return _Text;
        }
    }
    public void setOutline(bool show)
    {
        foreach (Outline outline in GetComponentsInChildren<Outline>())
        {
            outline.eraseRenderer = !show; // true is not show, false is show
        }
    }
    public void setOutline(int idx)
    {
        foreach (Outline outline in GetComponentsInChildren<Outline>())
        {
            outline.color = idx;
        }
    }

    PlayerUiIcon()
    {
        Instances.Add(this);
    }

    ~PlayerUiIcon()
    {
        Instances.Remove(this);
    }

    public void SetText(string text)
    {
        Text.text = text;
    }

    public void SetCallBack(Action cb)
    {
        callBack = cb;
    }

    public void OnHover()
    {
        IsHovered = true;
        PlayerUiIconHovered = this;
    }

    public void OnClick()
    {
        if (callBack != null) callBack();
    }
}
