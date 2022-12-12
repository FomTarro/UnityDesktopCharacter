using UnityEngine;
using UnityDesktopCharacter.Abstract;

namespace UnityDesktopCharacter.Example {
    public class ExampleUIManager : BaseUIManager<MenuType, Menu, ExampleUIManager> {
        public void OpenSettings(){
            this.OpenMenu(MenuType.SETTINGS);
        }

    }

    public enum MenuType {
        SETTINGS = 1,
    }

    [System.Serializable]
    public class Menu : Menu<MenuType>{ 
        public Texture2D icon;
    }
}