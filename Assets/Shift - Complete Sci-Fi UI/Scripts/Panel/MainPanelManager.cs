using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Michsky.UI.Shift
{
    public class MainPanelManager : MonoBehaviour
    {
        [Header("Panel List")]
        public List<PanelItem> panels = new List<PanelItem>();

        [Header("Settings")]
        [SerializeField] private bool useCulling = true;
        public int currentPanelIndex = 0;
        private int currentButtonIndex = 0;
        private int newPanelIndex;

        private GameObject currentPanel;
        private GameObject nextPanel;
        private GameObject currentButton;
        private GameObject nextButton;

        private Animator currentPanelAnimator;
        private Animator nextPanelAnimator;
        private Animator currentButtonAnimator;
        private Animator nextButtonAnimator;

        string panelFadeIn = "Panel In";
        string panelFadeOut = "Panel Out";
        string buttonFadeIn = "Normal to Pressed";
        string buttonFadeOut = "Pressed to Dissolve";
        string buttonFadeNormal = "Pressed to Normal";

        bool firstTime = true;

        [System.Serializable]
        public class PanelItem
        {
            public string panelName;
            public GameObject panelObject;
            public GameObject buttonObject;
        }

        void OnEnable()
        {
            if (firstTime == false && nextPanelAnimator != null && nextPanelAnimator.gameObject.activeInHierarchy)
            {
                nextPanelAnimator.Play(panelFadeIn);
                PlayButton(nextButtonAnimator, buttonFadeIn);
            }

            else if (firstTime == false && currentPanelAnimator != null && currentPanelAnimator.gameObject.activeInHierarchy)
            {
                currentPanelAnimator.Play(panelFadeIn);
                PlayButton(currentButtonAnimator, buttonFadeIn);
            }
        }

        void Awake()
        {
            currentButton = panels[currentPanelIndex].buttonObject;
            currentButtonAnimator = currentButton != null ? currentButton.GetComponent<Animator>() : null;
            PlayButton(currentButtonAnimator, buttonFadeIn);

            currentPanel = panels[currentPanelIndex].panelObject;
            currentPanelAnimator = currentPanel != null ? currentPanel.GetComponent<Animator>() : null;
            if (currentPanelAnimator != null)
                currentPanelAnimator.Play(panelFadeIn);
            ForcePanelVisible(currentPanel);

            firstTime = false;

            if (useCulling == false)
                return;

            StartCoroutine("DisablePreviousPanel");
        }

        public void OpenFirstTab()
        {
            if (currentPanelIndex != 0)
                OpenPanel(panels[0].panelName);
        }

        public void OpenPanel(string newPanel)
        {
            for (int i = 0; i < panels.Count; i++)
            {
                if (panels[i].panelName == newPanel)
                {
                    newPanelIndex = i;
                    break;
                }
            }

            if (newPanelIndex != currentPanelIndex)
            {
                StopCoroutine("DisablePreviousPanel");

                currentPanel = panels[currentPanelIndex].panelObject;
                currentPanelIndex = newPanelIndex;
                nextPanel = panels[currentPanelIndex].panelObject;
                nextPanel.SetActive(true);

                currentPanelAnimator = currentPanel != null ? currentPanel.GetComponent<Animator>() : null;
                nextPanelAnimator = nextPanel != null ? nextPanel.GetComponent<Animator>() : null;

                if (currentPanelAnimator != null)
                    currentPanelAnimator.Play(panelFadeOut);
                if (nextPanelAnimator != null)
                    nextPanelAnimator.Play(panelFadeIn);
                ForcePanelVisible(nextPanel);

                StartCoroutine("DisablePreviousPanel");

                currentButton = panels[currentButtonIndex].buttonObject;
                currentButtonIndex = newPanelIndex;
                nextButton = panels[currentButtonIndex].buttonObject;

                currentButtonAnimator = currentButton != null ? currentButton.GetComponent<Animator>() : null;
                nextButtonAnimator = nextButton != null ? nextButton.GetComponent<Animator>() : null;

                PlayButton(currentButtonAnimator, buttonFadeOut);
                PlayButton(nextButtonAnimator, buttonFadeIn);
            }
            else
            {
                // Already on this panel (e.g. Home at boot). Default state is Panel Out Helper
                // (alpha 0) — re-assert visible when tab buttons were removed.
                var panel = panels[currentPanelIndex].panelObject;
                if (panel != null)
                {
                    panel.SetActive(true);
                    var anim = panel.GetComponent<Animator>();
                    if (anim != null && anim.enabled)
                        anim.Play(panelFadeIn);
                    ForcePanelVisible(panel);
                }
            }
        }

        public void NextPage()
        {
            if (currentPanelIndex <= panels.Count - 2)
            {
                StopCoroutine("DisablePreviousPanel");

                currentPanel = panels[currentPanelIndex].panelObject;
                currentButton = panels[currentButtonIndex].buttonObject;
                nextButton = panels[currentButtonIndex + 1].buttonObject;
                currentPanel.gameObject.SetActive(true);

                currentPanelAnimator = currentPanel != null ? currentPanel.GetComponent<Animator>() : null;
                currentButtonAnimator = currentButton != null ? currentButton.GetComponent<Animator>() : null;

                PlayButton(currentButtonAnimator, buttonFadeNormal);
                if (currentPanelAnimator != null)
                    currentPanelAnimator.Play(panelFadeOut);

                currentPanelIndex += 1;
                currentButtonIndex += 1;
                nextPanel = panels[currentPanelIndex].panelObject;
                nextPanel.gameObject.SetActive(true);

                nextPanelAnimator = nextPanel != null ? nextPanel.GetComponent<Animator>() : null;
                nextButtonAnimator = nextButton != null ? nextButton.GetComponent<Animator>() : null;
                if (nextPanelAnimator != null)
                    nextPanelAnimator.Play(panelFadeIn);
                PlayButton(nextButtonAnimator, buttonFadeIn);
                ForcePanelVisible(nextPanel);
            }
        }

        public void PrevPage()
        {
            if (currentPanelIndex >= 1)
            {
                StopCoroutine("DisablePreviousPanel");

                currentPanel = panels[currentPanelIndex].panelObject;
                currentButton = panels[currentButtonIndex].buttonObject;
                nextButton = panels[currentButtonIndex - 1].buttonObject;
                currentPanel.gameObject.SetActive(true);

                currentPanelAnimator = currentPanel != null ? currentPanel.GetComponent<Animator>() : null;
                currentButtonAnimator = currentButton != null ? currentButton.GetComponent<Animator>() : null;

                PlayButton(currentButtonAnimator, buttonFadeNormal);
                if (currentPanelAnimator != null)
                    currentPanelAnimator.Play(panelFadeOut);

                currentPanelIndex -= 1;
                currentButtonIndex -= 1;
                nextPanel = panels[currentPanelIndex].panelObject;
                nextPanel.gameObject.SetActive(true);

                nextPanelAnimator = nextPanel != null ? nextPanel.GetComponent<Animator>() : null;
                nextButtonAnimator = nextButton != null ? nextButton.GetComponent<Animator>() : null;
                if (nextPanelAnimator != null)
                    nextPanelAnimator.Play(panelFadeIn);
                PlayButton(nextButtonAnimator, buttonFadeIn);
                ForcePanelVisible(nextPanel);
            }
        }

        IEnumerator DisablePreviousPanel()
        {
            yield return new WaitForSecondsRealtime(0.5f);
            for (int i = 0; i < panels.Count; i++)
            {
                if (i == currentPanelIndex)
                    continue;

                if (panels[i].panelObject != null)
                    panels[i].panelObject.gameObject.SetActive(false);
            }
        }

        static void PlayButton(Animator anim, string state)
        {
            if (anim != null && anim.enabled)
                anim.Play(state);
        }

        /// <summary>Panel controllers default to Out Helper (alpha 0). When tab buttons are
        /// absent, Awake used to NRE before Panel In — leave the CanvasGroup usable.</summary>
        static void ForcePanelVisible(GameObject panel)
        {
            if (panel == null) return;
            var group = panel.GetComponent<CanvasGroup>();
            if (group == null) return;
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
        }
    }
}
