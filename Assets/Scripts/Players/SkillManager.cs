using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Unity.Netcode.Components;

public class SkillManager : NetworkBehaviour
{
    [Header("Configuration")]
    [SerializeField] private ASkills[] skillsList;

    private NetworkVariable<int> netCurrentSkillIndex = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private PlayerInput inputActions;
    private ASkills currentSkillInstance;

    private void Awake()
    {
        inputActions = GetComponentInParent<PlayerInput>();
    }

    public void UpdateActiveSkillAnimation(float speed, bool isJumpTrigger)
    {
        if (currentSkillInstance != null)
        {
            currentSkillInstance.UpdateAnimationState(speed, isJumpTrigger);
        }
    }

    public override void OnNetworkSpawn()
    {
        netCurrentSkillIndex.OnValueChanged += OnSkillIndexChanged;

        EquipSkillLocal(netCurrentSkillIndex.Value);

        if (IsOwner)
        {
            SubscribeToInputs();
        }
    }

    public override void OnNetworkDespawn()
    {
        netCurrentSkillIndex.OnValueChanged -= OnSkillIndexChanged;

        if (IsOwner)
        {
            UnsubscribeFromInputs();
        }
    }

    private void OnSkillIndexChanged(int previousIndex, int newIndex)
    {
        EquipSkillLocal(newIndex);
    }

    #region Input Management (Owner Only)

    private void SubscribeToInputs()
    {
        if (inputActions == null) return;

        inputActions.actions["NextSkill"].performed += OnNextSkill;
        inputActions.actions["PreviousSkill"].performed += OnPrevSkill;
        inputActions.actions["MainAction"].performed += OnMainAction;
        inputActions.actions["SecondaryAction"].performed += OnSecondaryAction;
    }

    private void UnsubscribeFromInputs()
    {
        if (inputActions == null) return;

        inputActions.actions["NextSkill"].performed -= OnNextSkill;
        inputActions.actions["PreviousSkill"].performed -= OnPrevSkill;
        inputActions.actions["MainAction"].performed -= OnMainAction;
        inputActions.actions["SecondaryAction"].performed -= OnSecondaryAction;
    }

    private void OnNextSkill(InputAction.CallbackContext ctx) => ChangeSkill(1);
    private void OnPrevSkill(InputAction.CallbackContext ctx) => ChangeSkill(-1);

    private void OnMainAction(InputAction.CallbackContext ctx)
    {
        if (currentSkillInstance != null && !currentSkillInstance.IsSkillLocked())
            currentSkillInstance.MainAction();
    }

    private void OnSecondaryAction(InputAction.CallbackContext ctx)
    {
        if (currentSkillInstance != null && !currentSkillInstance.IsSkillLocked())
            currentSkillInstance.SecondaryAction();
    }

    #endregion

    #region Skill Logic

    private void ChangeSkill(int direction)
    {
        if (currentSkillInstance != null && currentSkillInstance.IsSkillLocked()) return;

        int newIndex = netCurrentSkillIndex.Value;
        int attempts = 0;
        bool foundFreeSkill = false;

        while (attempts < skillsList.Length)
        {
            newIndex = (newIndex + direction) % skillsList.Length;
            if (newIndex < 0) newIndex += skillsList.Length;

            if (!IsSkillTakenByOthers(newIndex))
            {
                foundFreeSkill = true;
                break;
            }
            
            attempts++;
        }

        if (foundFreeSkill && newIndex != netCurrentSkillIndex.Value)
        {
            RequestChangeSkillServerRpc(newIndex);
        }
        else
        {
            Debug.Log("Aucun autre skill disponible !");
        }
    }

    private bool IsSkillTakenByOthers(int indexToCheck)
    {
        SkillManager[] allPlayers = FindObjectsByType<SkillManager>(FindObjectsSortMode.None);

        foreach (var player in allPlayers)
        {
            if (player == this) continue;

            if (player.netCurrentSkillIndex.Value == indexToCheck)
            {
                return true;
            }
        }
        return false;
    }

    [ServerRpc]
    private void RequestChangeSkillServerRpc(int newIndex)
    {
        netCurrentSkillIndex.Value = newIndex;
    }

    private void EquipSkillLocal(int skillIndex)
    {
        if (skillIndex < 0 || skillIndex >= skillsList.Length) return;

        if (currentSkillInstance != null)
        {
            currentSkillInstance.DeactivateSkill();
        }

        currentSkillInstance = skillsList[skillIndex];
        
        GetComponent<NetworkTransform>().transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z);

        currentSkillInstance.ActivateSkill();
    }

    #endregion
}