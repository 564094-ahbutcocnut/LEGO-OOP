using LEGOModelImporter;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.LEGO.Behaviours.Actions
{
    public class ExplodeAction : Action
    {
        [SerializeField, Range(1,50), Tooltip("The power of the explosion.")]
        uint m_Power = 10;

        [SerializeField, Tooltip("Remove bricks shortly after the explosion.")]
        bool m_RemoveBricks = false;

        bool m_Detonated;

        List<LEGOBehaviour> m_Behaviours = new List<LEGOBehaviour>();

        HashSet<Brick> m_ExplodingBricks = new HashSet<Brick>();
        Bounds m_ExplodingBounds;

        protected override void Reset()
        {
            base.Reset();

            m_IconPath = "Assets/LEGO/Gizmos/LEGO Behaviour Icons/Explode Action.png";
        }

        protected override void Start()
        {
            base.Start();

            if (IsPlacedOnBrick())
            {
                // Movement actions move the whole ModelGroup, but connected-brick scope
                // can miss bricks in that group. Explode the same set that moves.
                m_ExplodingBricks = new HashSet<Brick>(m_ScopedBricks);
                if (m_Scope == Scope.ConnectedBricks)
                {
                    var group = m_Brick.GetComponentInParent<ModelGroup>();
                    if (group)
                    {
                        m_ExplodingBricks.UnionWith(group.GetComponentsInChildren<Brick>(true));
                    }
                }
                m_ExplodingBounds = GetScopedBounds(m_ExplodingBricks, out _, out _);

                // Find all LEGOBehaviours in the exploding set.
                foreach (var brick in m_ExplodingBricks)
                {
                    m_Behaviours.AddRange(brick.GetComponentsInChildren<LEGOBehaviour>());
                }
            }
        }

        protected void Update()
        {
            if (m_Active)
            {
                if (!m_Detonated)
                {
                    // Remove all game objects with LEGOBehaviourCollider components.
                    foreach (var brick in m_ExplodingBricks)
                    {
                        foreach (var behaviourCollider in brick.GetComponentsInChildren<LEGOBehaviourCollider>())
                        {
                            Destroy(behaviourCollider.gameObject);
                        }

                        // Restore part's original colliders.
                        foreach(var part in brick.parts)
                        {
                            BrickColliderCombiner.RestoreOriginalColliders(part);
                        }
                    }

                    var lift = m_Power * 0.25f;

                    // Send all bricks in scope flying.
                    foreach (var brick in m_ExplodingBricks)
                    {
                        brick.DisconnectAll();

                        var rigidBody = brick.gameObject.GetComponent<Rigidbody>();
                        if (!rigidBody)
                        {
                            rigidBody = brick.gameObject.AddComponent<Rigidbody>();
                        }
                        rigidBody.AddExplosionForce(m_Power, transform.position + transform.TransformVector(m_BrickPivotOffset), m_ExplodingBounds.extents.magnitude, lift, ForceMode.VelocityChange);

                        if (m_RemoveBricks)
                        {
                            brick.gameObject.AddComponent<BlinkAndDisable>();
                        }
                    }

                    PlayAudio(moveWithScope: false, destroyWithAction: false);

                    // Delay destruction of LEGOBehaviours one frame to allow multiple Explode Actions to detonate.
                    m_Detonated = true;
                }
                else
                {
                    // Destroy all the LEGOBehaviours in scope (including this script).
                    foreach (var behaviour in m_Behaviours)
                    {
                        if (behaviour)
                        {
                            Destroy(behaviour);
                        }
                    }
                }
            }
        }
    }
}
