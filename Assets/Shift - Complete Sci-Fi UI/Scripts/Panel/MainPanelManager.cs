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
            // Station panels keep Animators disabled (Panel Out Helper zeros alpha). Re-assert.
            var panel = nextPanel != null ? nextPanel : currentPanel;
            if (panel != null && panel.activeInHierarchy)
                ForcePanelVisible(panel);
        }

        void Awake()
        {
            currentPanel = panels[currentPanelIndex].panelObject;
            ForcePanelVisible(currentPanel);

            currentButton = panels[currentPanelIndex].buttonObject;
            currentButtonAnimator = ButtonAnimator(currentButton, currentPanel);
            PlayButton(currentButtonAnimator, buttonFadeIn);

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
            int found = -1;
            for (int i = 0; i < panels.Count; i++)
            {
                if (panels[i].panelName == newPanel)
                {
                    found = i;
                    break;
                }
            }
            if (found < 0 || panels[found].panelObject == null)
                return;

            newPanelIndex = found;

            if (newPanelIndex != currentPanelIndex)
            {
                StopCoroutine("DisablePreviousPanel");

                currentPanel = panels[currentPanelIndex].panelObject;
                currentPanelIndex = newPanelIndex;
                nextPanel = panels[currentPanelIndex].panelObject;
                if (nextPanel == null) return;
                nextPanel.SetActive(true);

                ForcePanelVisible(nextPanel);

                StartCoroutine("DisablePreviousPanel");

                currentButton = panels[currentButtonIndex].buttonObject;
                currentButtonIndex = newPanelIndex;
                nextButton = panels[currentButtonIndex].buttonObject;

                // Station panels wire buttonObject to the panel itself (no tab bar). Skip
                // tab-button anims when buttonObject == panelObject.
                currentButtonAnimator = ButtonAnimator(currentButton, currentPanel);
                nextButtonAnimator = ButtonAnimator(nextButton, nextPanel);

                PlayButton(currentButtonAnimator, buttonFadeOut);
                PlayButton(nextButtonAnimator, buttonFadeIn);
            }
            else
            {
                var panel = panels[currentPanelIndex].panelObject;
                if (panel != null)
                {
                    panel.SetActive(true);
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

                nextButtonAnimator = nextButton != null ? nextButton.GetComponent<Animator>() : null;
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

        /// <summary>Null when the "button" is the panel GameObject — there is no tab chrome.</summary>
        static Animator ButtonAnimator(GameObject button, GameObject panel)
        {
            if (button == null || button == panel) return null;
            return button.GetComponent<Animator>();
        }

        /// <summary>Panel controllers default to Out Helper (alpha 0) and keep writing that
        /// every frame. Disable the Animator when forcing visibility so alpha sticks.</summary>
        static void ForcePanelVisible(GameObject panel)
        {
            if (panel == null) return;
            var anim = panel.GetComponent<Animator>();
            if (anim != null) anim.enabled = false;
            var group = panel.GetComponent<CanvasGroup>();
            if (group == null) return;
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
        }
    }
}
