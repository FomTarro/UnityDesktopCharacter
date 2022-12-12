using System.Collections;
using System.Collections.Generic;
using UnityDesktopCharacter.Abstract;
using UnityEngine;

namespace UnityDesktopCharacter.Example {
	public class ExampleCharacterManager : BaseCharacterManager<ExampleCharacter, ExampleCharacterManager> {
		public override void Initialize() {
			Debug.Log("Character Manager Initialized!");
		}

        public void MakeCharacterWander(){
            this.Character.WalkToRandomSpot();
        }

	}
}
