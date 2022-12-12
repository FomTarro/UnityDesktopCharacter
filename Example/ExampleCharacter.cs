using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityDesktopCharacter.Abstract;

namespace UnityDesktopCharacter.Example {

	public class ExampleCharacter : BaseCharacter {

        [SerializeField]
        private WalkingController _walkingController = null;

        public void WalkToRandomSpot(){
            Vector3 destination = this._walkingController.GetRandomDestinationOnBoundary();
            this._walkingController.StartLocomotion(destination);
        }

	}
}
