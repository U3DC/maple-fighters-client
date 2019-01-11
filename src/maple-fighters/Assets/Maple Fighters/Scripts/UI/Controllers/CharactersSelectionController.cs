﻿using System.Threading.Tasks;
using CommonTools.Coroutines;
using CommonTools.Log;
using Game.Common;
using Scripts.Containers;
using Scripts.Services;
using Scripts.UI.Core;
using Scripts.UI.Windows;
using Scripts.Utils;

namespace Scripts.UI.Controllers
{
    public class CharactersSelectionController : MonoSingleton<CharactersSelectionController>
    {
        private ExternalCoroutinesExecutor coroutinesExecutor;
        private CharactersSelectionWindow charactersSelectionWindow;
        private CharacterNameWindow characterNameWindow;
        private ClickableCharacter clickedCharacter;
        private CreateCharacterRequestParameters characterRequestParameters;

        protected override void OnAwake()
        {
            base.OnAwake();

            coroutinesExecutor = new ExternalCoroutinesExecutor();
        }

        private void Update()
        {
            coroutinesExecutor?.Update();
        }

        public void ShowCharactersSelectionWindow(ClickableCharacter clickableCharacter, int characterIndex)
        {
            clickedCharacter = clickableCharacter;
            clickedCharacter.PlayWalkAnimationAction.Invoke();

            characterRequestParameters.Index = (CharacterIndex)characterIndex;

            if (charactersSelectionWindow)
            {
                charactersSelectionWindow.Show();
            }
            else
            {
                charactersSelectionWindow = UserInterfaceContainer.GetInstance().Add<CharactersSelectionWindow>();
                charactersSelectionWindow.Show();

                SubscribeToCharactersSelectionWindowEvents();
            }
        }

        private void ShowCharacterNamwWindow()
        {
            if (characterNameWindow)
            {
                characterNameWindow.Show();
            }
            else
            {
                characterNameWindow = UserInterfaceContainer.GetInstance().Add<CharacterNameWindow>();
                characterNameWindow.Show();

                SubscribeToCharacterNameWindow();
            }
        }

        protected override void OnDestroying()
        {
            base.OnDestroying();

            if (charactersSelectionWindow != null)
            {
                UnsubscribeFromCharactersSelectionWindowEvents();

                UserInterfaceContainer.GetInstance()?.Remove(charactersSelectionWindow);
            }

            if (characterNameWindow != null)
            {
                UnsubscribeFromCharacterNameWindow();

                UserInterfaceContainer.GetInstance()?.Remove(characterNameWindow);
            }

            coroutinesExecutor?.Dispose();
        }

        private void SubscribeToCharactersSelectionWindowEvents()
        {
            charactersSelectionWindow.ChoosedClicked += OnChoosedClass;
            charactersSelectionWindow.CancelClicked += OnCancelClicked;
            charactersSelectionWindow.KnightSelected += OnKnightSelected;
            charactersSelectionWindow.ArrowSelected += OnArrowSelected;
            charactersSelectionWindow.WizardSelected += OnWizardSelected;
            charactersSelectionWindow.Deselected += OnDeselected;
        }

        private void UnsubscribeFromCharactersSelectionWindowEvents()
        {
            charactersSelectionWindow.ChoosedClicked -= OnChoosedClass;
            charactersSelectionWindow.CancelClicked -= OnCancelClicked;
            charactersSelectionWindow.KnightSelected -= OnKnightSelected;
            charactersSelectionWindow.ArrowSelected -= OnArrowSelected;
            charactersSelectionWindow.WizardSelected -= OnWizardSelected;
            charactersSelectionWindow.Deselected -= OnDeselected;
        }

        private void SubscribeToCharacterNameWindow()
        {
            characterNameWindow.ConfirmClicked += OnConfirmClicked;
            characterNameWindow.BackClicked += OnBackClicked;
        }

        private void UnsubscribeFromCharacterNameWindow()
        {
            characterNameWindow.ConfirmClicked -= OnConfirmClicked;
            characterNameWindow.BackClicked -= OnBackClicked;
        }

        private void OnConfirmClicked(string characterName)
        {
            characterRequestParameters.Name = characterName;

            var noticeWindow = Utils.ShowNotice("Creating a new character... Please wait.", ShowCharacterNamwWindow, true);
            noticeWindow.OkButton.interactable = false;

            coroutinesExecutor.StartTask(CreateCharacter, exception => ServiceConnectionProviderUtils.OnOperationFailed());
        }

        private async Task CreateCharacter(IYield yield)
        {
            var characterPeerLogic = ServiceContainer.GameService.GetPeerLogic<ICharacterPeerLogicAPI>().AssertNotNull();
            var responseParameters = await characterPeerLogic.CreateCharacter(yield, characterRequestParameters);
            switch (responseParameters.Status)
            {
                case CharacterCreationStatus.Succeed:
                {
                    CharactersController.GetInstance().RecreateCharacter(GetLastCreatedCharacter());

                    var noticeWindow = UserInterfaceContainer.GetInstance().Get<NoticeWindow>().AssertNotNull();
                    noticeWindow.Message.text = "Character created successfully.";
                    noticeWindow.OkButtonClickedAction = charactersSelectionWindow.DeactiveAll;
                    noticeWindow.OkButton.interactable = true;
                    break;
                }

                case CharacterCreationStatus.Failed:
                {
                    var noticeWindow = UserInterfaceContainer.GetInstance().Get<NoticeWindow>().AssertNotNull();
                    noticeWindow.Message.text = "Failed to create a new character, please try again.";
                    noticeWindow.OkButton.interactable = true;
                    break;
                }

                case CharacterCreationStatus.NameUsed:
                {
                    var noticeWindow = UserInterfaceContainer.GetInstance().Get<NoticeWindow>().AssertNotNull();
                    noticeWindow.Message.text = "The name is already in use, choose another name.";
                    noticeWindow.OkButton.interactable = true;
                    break;
                }

                default:
                {
                    var noticeWindow = UserInterfaceContainer.GetInstance().Get<NoticeWindow>().AssertNotNull();
                    noticeWindow.Message.text = "Something went wrong, please try again.";
                    noticeWindow.OkButton.interactable = true;
                    break;
                }
            }
        }

        private void OnBackClicked()
        {
            charactersSelectionWindow.Show();
        }

        private void OnChoosedClass()
        {
            ShowCharacterNamwWindow();
        }

        private void OnCancelClicked()
        {
            clickedCharacter.PlayIdleAnimationAction.Invoke();
        }

        private void OnKnightSelected()
        {
            characterRequestParameters.CharacterClass = CharacterClasses.Knight;
        }

        private void OnArrowSelected()
        {
            characterRequestParameters.CharacterClass = CharacterClasses.Arrow;
        }

        private void OnWizardSelected()
        {
            characterRequestParameters.CharacterClass = CharacterClasses.Wizard;
        }

        private void OnDeselected()
        {
            // Left blank intentionally
        }

        private CharacterParameters GetLastCreatedCharacter()
        {
            return new CharacterParameters(characterRequestParameters.Name, characterRequestParameters.CharacterClass, characterRequestParameters.Index);
        }
    }
}