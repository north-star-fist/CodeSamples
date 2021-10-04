using System;
using System.Collections.Generic;

using UnityEngine;

namespace Sergey.Safonov.Actors {

    public class RagdollSwitcher : ActorComponent {

        [Tooltip("Fill the array with ragdoll rigidbodies and related colliders")]
        public RigidbodyWithColliders[] ragdollParts;

        [Tooltip("Check the glag to activate ragdoll physics and uncheck to deactivate it")]
        public bool activate = true;

        List<RagdollPart> parts = new List<RagdollPart>();

        void Awake() {
            if (ragdollParts != null && ragdollParts.Length > 0) {
                Array.ForEach(ragdollParts, rdp => parts.Add(createRagdollPart(rdp)));
            }
        }


        public override void Act() {
            if (activate) {
                activateRagdoll();
            } else {
                deactivateRagdoll();
            }
        }

        private void activateRagdoll() {
            //adding rigidbodies
            foreach (var ragdollPart in parts) {
                //add rigidbody
                AddRigidbody(ragdollPart.obj, ragdollPart.rBody);
                //enabling colliders
                Array.ForEach(ragdollPart.colliders, c => c.enabled = true);
            }

            //second pass to set connections and switch joints on (should not be too slow)
            foreach (var ragdollPart in parts) {
                if (ragdollPart.joint != null) {
                    CharacterJoint joint = AddCharacterJoint(ragdollPart.joint, ragdollPart.obj);
                    joint.connectedBody = ragdollPart.connectedObj.GetComponent<Rigidbody>();
                }
            }
        }

        private void deactivateRagdoll() {
            foreach (var rBodyPart in parts) {
                //colliders switching off
                Array.ForEach(rBodyPart.colliders, c => c.enabled = false);
                //joint destruction
                Destroy(rBodyPart.obj.GetComponent<CharacterJoint>());
                //rigidbody destruction
                Destroy(rBodyPart.obj.GetComponent<Rigidbody>());
            }
        }

        private static RagdollPart createRagdollPart(RigidbodyWithColliders ragdollWColliders) {
            Collider[] colliders = ragdollWColliders.colliders;
            Collider[] physColliders = Array.FindAll(colliders, c => !c.isTrigger);
            Rigidbody rb = ragdollWColliders.rigidbody;
            RigidbodyStruct rBodyStruct = new RigidbodyStruct(rb);
            RagdollPart ragdollPart = new RagdollPart(rb.gameObject, rBodyStruct, physColliders);

            CharacterJoint srcJoint = rb.gameObject.GetComponent<CharacterJoint>();
            if (srcJoint != null) {
                ragdollPart.joint = new CharacterJointStruct(srcJoint);
                ragdollPart.connectedObj = srcJoint.connectedBody.gameObject;
            }

            return ragdollPart;
        }


        private void AddRigidbody(GameObject obj, RigidbodyStruct rBodyStruct) {
            Rigidbody rBody = obj.GetComponent<Rigidbody>();
            if (rBody != null)
            {
                Debug.LogWarningFormat(this, "Rigidbody is already attached to the object {0}", obj);
            } else
            {
                rBody = obj.AddComponent<Rigidbody>();
            }
            
            rBody.mass = rBodyStruct.mass;
            rBody.drag = rBodyStruct.drag;
            rBody.angularDrag = rBodyStruct.angularDrag;
            rBody.useGravity = rBodyStruct.useGravity;
            rBody.isKinematic = rBodyStruct.isKinematic;
            rBody.interpolation = rBodyStruct.interpolation;
            rBody.collisionDetectionMode = rBodyStruct.collisionDetectionMode;
            rBody.constraints = rBodyStruct.constraints;
        }

        private CharacterJoint AddCharacterJoint(CharacterJointStruct joint, GameObject obj) {
            CharacterJoint charJoint = obj.AddComponent<CharacterJoint>();

            CopyJointFields(joint, charJoint);

            return charJoint;
        }

        private void CopyJointFields(CharacterJointStruct jointStruct, CharacterJoint charJoint) {
            charJoint.anchor = jointStruct.anchor;
            charJoint.axis = jointStruct.axis;
            charJoint.autoConfigureConnectedAnchor = jointStruct.autoConfigureConnectedAnchor;

            charJoint.connectedAnchor = jointStruct.connectedAnchor;
            charJoint.swingAxis = jointStruct.swingAxis;
            charJoint.twistLimitSpring = jointStruct.twistLimitSpring;

            charJoint.lowTwistLimit = jointStruct.lowTwistLimit;
            charJoint.highTwistLimit = jointStruct.highTwistLimit;
            charJoint.swingLimitSpring = jointStruct.swingLimitSpring;

            charJoint.swing1Limit = jointStruct.swing1Limit;
            charJoint.swing2Limit = jointStruct.swing2Limit;
            charJoint.enableProjection = jointStruct.enableProjection;

            charJoint.projectionDistance = jointStruct.projectionDistance;
            charJoint.projectionAngle = jointStruct.projectionAngle;
            charJoint.breakForce = jointStruct.breakForce;
            charJoint.breakTorque = jointStruct.breakTorque;

            charJoint.enableCollision = jointStruct.enableCollision;
            charJoint.enablePreprocessing = jointStruct.enablePreprocessing;
            charJoint.massScale = jointStruct.massScale;
            charJoint.connectedMassScale = jointStruct.connectedMassScale;
        }

        class RagdollPart {
            //Body owner
            public GameObject obj;
            public RigidbodyStruct rBody;
            public Collider[] colliders;
            public CharacterJointStruct joint;
            public GameObject connectedObj;


            public RagdollPart(GameObject gameObject, RigidbodyStruct ragdollBody, Collider[] physColliders) {
                this.obj = gameObject;
                this.rBody = ragdollBody;
                this.colliders = physColliders;
            }
        }

        struct RigidbodyStruct {
            public float mass;
            public float drag;
            public float angularDrag;
            public bool useGravity;
            public bool isKinematic;
            public RigidbodyInterpolation interpolation;
            public CollisionDetectionMode collisionDetectionMode;
            public RigidbodyConstraints constraints;

            public RigidbodyStruct(Rigidbody srcBody) {
                this.mass = srcBody.mass;
                this.drag = srcBody.drag;
                this.angularDrag = srcBody.angularDrag;
                this.useGravity = srcBody.useGravity;
                this.isKinematic = srcBody.isKinematic;
                this.interpolation = srcBody.interpolation;
                this.collisionDetectionMode = srcBody.collisionDetectionMode;
                this.constraints = srcBody.constraints;
            }
        }

        class CharacterJointStruct {
            public Vector3 anchor;
            public Vector3 axis;
            public bool autoConfigureConnectedAnchor;
            public Vector3 connectedAnchor;
            public Vector3 swingAxis;
            public SoftJointLimitSpring twistLimitSpring;
            public SoftJointLimit lowTwistLimit;
            public SoftJointLimit highTwistLimit;
            public SoftJointLimitSpring swingLimitSpring;
            public SoftJointLimit swing1Limit;
            public SoftJointLimit swing2Limit;
            public bool enableProjection;
            public float projectionDistance;
            public float projectionAngle;
            public float breakForce;
            public float breakTorque;
            public bool enableCollision;
            public bool enablePreprocessing;
            public float massScale;
            public float connectedMassScale;

            public CharacterJointStruct(CharacterJoint srcJoint) {

                this.anchor = srcJoint.anchor;
                this.axis = srcJoint.axis;
                this.autoConfigureConnectedAnchor = srcJoint.autoConfigureConnectedAnchor;

                this.connectedAnchor = srcJoint.connectedAnchor;
                this.swingAxis = srcJoint.swingAxis;
                this.twistLimitSpring = srcJoint.twistLimitSpring;

                this.lowTwistLimit = srcJoint.lowTwistLimit;
                this.highTwistLimit = srcJoint.highTwistLimit;
                this.swingLimitSpring = srcJoint.swingLimitSpring;

                this.swing1Limit = srcJoint.swing1Limit;
                this.swing2Limit = srcJoint.swing2Limit;
                this.enableProjection = srcJoint.enableProjection;

                this.projectionDistance = srcJoint.projectionDistance;
                this.projectionAngle = srcJoint.projectionAngle;
                this.breakForce = srcJoint.breakForce;
                this.breakTorque = srcJoint.breakTorque;

                this.enableCollision = srcJoint.enableCollision;
                this.enablePreprocessing = srcJoint.enablePreprocessing;
                this.massScale = srcJoint.massScale;
                this.connectedMassScale = srcJoint.connectedMassScale;
            }
        }

        [Serializable]
        public class RigidbodyWithColliders
        {
            public Rigidbody rigidbody;
            public Collider[] colliders;
        }
    }
}