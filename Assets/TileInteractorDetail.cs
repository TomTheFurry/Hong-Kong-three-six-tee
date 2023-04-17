using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TileInteractorDetail : MonoBehaviour
{
    public Button Select;
    public Button Cancel;

    private Action _OnClick = null;

    private void Start()
    {
        Cancel.onClick.AddListener(() => gameObject.SetActive(false));
        Select.onClick.AddListener(() => {
            if (_OnClick != null)
                _OnClick();

            _OnClick = null;
            Debug.Log("Clicked");

            gameObject.SetActive(false);
        });
    }

    public void SetOnClick(Action cb)
    {
        gameObject.SetActive(true);
        _OnClick = cb;
    }
}
