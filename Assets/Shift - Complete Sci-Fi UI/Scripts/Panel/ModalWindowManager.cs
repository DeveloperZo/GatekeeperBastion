using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Michsky.UI.Shift
{
    public class ModalWindowManager : MonoBehaviour
    {
        [Header("Resources")]
        public TextMeshProUGUI windowTitle;
        public TextMeshProUGUI windowDescription;

        [Header("Settings")]
        public bool sharpAnimations = false;
        public bool useCustomTexts = false;
        public string titleText = "Title";
        [TextArea] public string descriptionText = "Description here";

        Animator mWindowAnimator;
        bool isOn = false;

        void Start()
        {
            EnsureWindowAnimator();

            if (useCustomTexts == false)
            {
                if (windowTitle != null)
                    windowTitle.text = titleText;
                if (windowDescription != null)
                    windowDescription.text = descriptionText;
            }

            // Hide at boot. Skip when ModalWindowIn already opened us — inactive-at-load
            // means Start runs after In, and SetActive(false) would close the window.
            if (!isOn)
                gameObject.SetActive(false);
        }

        public void ModalWindowIn()
        {
            StopCoroutine("DisableWindow");
            gameObject.SetActive(true);

            if (isOn == false)
            {
                EnsureWindowAnimator();
                if (mWindowAnimator != null)
                {
                    if (sharpAnimations == false)
                        mWindowAnimator.CrossFade("Window In", 0.1f);
                    else
                        mWindowAnimator.Play("Window In");
                }

                isOn = true;
            }
        }

        public void ModalWindowOut()
        {
            if (isOn == true)
            {
                EnsureWindowAnimator();
                if (mWindowAnimator != null)
                {
                    if (sharpAnimations == false)
                        mWindowAnimator.CrossFade("Window Out", 0.1f);
                    else
                        mWindowAnimator.Play("Window Out");
                }

                isOn = false;
            }

            StartCoroutine("DisableWindow");
        }

        void EnsureWindowAnimator()
        {
            if (mWindowAnimator == null)
                mWindowAnimator = gameObject.GetComponent<Animator>();
        }

        IEnumerator DisableWindow()
        {
            yield return new WaitForSeconds(0.5f);
            gameObject.SetActive(false);
        }
    }
}