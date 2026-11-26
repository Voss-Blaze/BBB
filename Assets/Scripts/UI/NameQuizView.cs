using System;
using System.Collections;
using System.Collections.Generic;
using Lingjuan.Audio;
using Lingjuan.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Lingjuan.UI
{
    /// <summary>异兽辨名：从若干名字中选出正确的一个，答对才可将残页入库。</summary>
    public class NameQuizView : MonoBehaviour
    {
        public GameDatabase database;
        public Text nameText;
        public Image portraitImage;
        public Button startButton;
        public Button backButton;
        public Button[] optionButtons;
        public Text[] optionTexts;

        private readonly List<string> _options = new List<string>();
        private BeastDefinition _beast;
        private Action<bool> _onDone;
        private bool _answered;

        private void Awake()
        {
            if (startButton != null)
            {
                startButton.enabled = false;
            }

            if (backButton != null)
            {
                backButton.onClick.AddListener(Cancel);
            }

            if (optionButtons != null)
            {
                for (int i = 0; i < optionButtons.Length; i++)
                {
                    int index = i;
                    if (optionButtons[i] != null)
                    {
                        optionButtons[i].onClick.AddListener(() => Choose(index));
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (backButton != null)
            {
                backButton.onClick.RemoveListener(Cancel);
            }
        }

        public void Open(BeastDefinition beast, Action<bool> onDone)
        {
            if (beast == null)
            {
                return;
            }

            _beast = beast;
            _onDone = onDone;
            _answered = false;

            if (nameText != null)
            {
                nameText.text = "？？？";
            }

            if (portraitImage != null)
            {
                Sprite portrait = beast.portraitSprite != null ? beast.portraitSprite : beast.codexSprite;
                portraitImage.sprite = portrait;
                portraitImage.color = Color.white;
            }

            BuildOptions();
            ShowOptions();

            UiPanelAnimator.Show(gameObject);
        }

        private void BuildOptions()
        {
            _options.Clear();
            _options.Add(_beast.displayName);

            int target = optionButtons != null ? optionButtons.Length : _options.Count;
            if (database != null && database.config != null)
            {
                target = Mathf.Min(target, Mathf.Max(2, database.config.quizOptionCount));
            }

            int need = target - 1;
            if (need > 0)
            {
                List<string> pool = CollectDistractors();
                for (int i = 0; i < pool.Count && _options.Count < target; i++)
                {
                    _options.Add(pool[i]);
                }
            }

            for (int i = _options.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                string temp = _options[i];
                _options[i] = _options[j];
                _options[j] = temp;
            }
        }

        private List<string> CollectDistractors()
        {
            var result = new List<string>();

            if (database != null && database.namePool != null)
            {
                foreach (NameEntry entry in database.namePool.GetDistractors(_beast.displayName, 32))
                {
                    if (entry != null && !string.IsNullOrEmpty(entry.displayName))
                    {
                        result.Add(entry.displayName);
                    }
                }
            }

            if (result.Count == 0 && database != null)
            {
                foreach (BeastDefinition other in database.AllBeasts)
                {
                    if (other != null && other != _beast && other.displayName != _beast.displayName)
                    {
                        result.Add(other.displayName);
                    }
                }
            }

            for (int i = result.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                string temp = result[i];
                result[i] = result[j];
                result[j] = temp;
            }

            return result;
        }

        private void ShowOptions()
        {
            if (optionButtons != null)
            {
                for (int i = 0; i < optionButtons.Length; i++)
                {
                    bool valid = optionButtons[i] != null && i < _options.Count;
                    if (optionButtons[i] != null)
                    {
                        optionButtons[i].gameObject.SetActive(valid);
                    }

                    if (valid && optionTexts != null && i < optionTexts.Length && optionTexts[i] != null)
                    {
                        optionTexts[i].text = _options[i];
                    }
                }
            }
        }

        private void Choose(int index)
        {
            if (_answered || _beast == null || index < 0 || index >= _options.Count)
            {
                return;
            }

            _answered = true;
            bool correct = _options[index] == _beast.displayName;
            AudioManager.PlaySfx(correct ? SfxId.Reward : SfxId.Escape);

            if (nameText != null)
            {
                nameText.text = _beast.displayName;
            }

            StartCoroutine(FinishRoutine(correct));
        }

        private IEnumerator FinishRoutine(bool correct)
        {
            yield return new WaitForSeconds(correct ? 1.2f : 0.8f);

            UiPanelAnimator.Hide(gameObject);
            Action<bool> callback = _onDone;
            _onDone = null;
            callback?.Invoke(correct);
        }

        private void Cancel()
        {
            if (_answered)
            {
                return;
            }

            AudioManager.PlaySfx(SfxId.UiClick);
            _answered = true;
            UiPanelAnimator.Hide(gameObject);
            Action<bool> callback = _onDone;
            _onDone = null;
            callback?.Invoke(false);
        }
    }
}
