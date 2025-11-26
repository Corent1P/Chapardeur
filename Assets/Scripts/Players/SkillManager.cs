using UnityEngine;
using UnityEngine.InputSystem;

public class SkillManager : MonoBehaviour
{
    [SerializeField] private int currentSkillIndex = 0;
    [SerializeField] private ASkills[] skillsList;
    private PlayerInput inputActions;
    private ASkills currentSkill;

    private void Awake()
    {
        inputActions = GetComponentInParent<PlayerInput>();
    }

    private void OnEnable()
    {
        // inputActions.PlayerControls.Enable();
        InputAction nextAction = inputActions.actions["NextSkill"];
        InputAction prevAction = inputActions.actions["PreviousSkill"];

        nextAction.performed += ctx => NextSkill();
        prevAction.performed += ctx => PreviousSkill();
        EquipSkill(currentSkillIndex = 0);
    }

    private void OnDisable()
    {
        // inputActions.PlayerControls.Disable();

        InputAction nextAction = inputActions.actions["NextSkill"];
        InputAction prevAction = inputActions.actions["PreviousSkill"];
        InputAction mainAction = inputActions.actions["MainAction"];
        InputAction secondaryAction = inputActions.actions["SecondaryAction"];

        nextAction.performed -= ctx => NextSkill();
        prevAction.performed -= ctx => PreviousSkill();

        if (currentSkill != null)
        {
            mainAction.performed -= ctx => currentSkill.MainAction();
            secondaryAction.performed -= ctx => currentSkill.SecondaryAction();
        }
    }


    private void EquipSkill(int skillIndex)
    {
        if (skillIndex >= 0 && skillIndex < skillsList.Length)
        {
            InputAction mainAction = inputActions.actions["MainAction"];
            InputAction secondaryAction = inputActions.actions["SecondaryAction"];

            transform.position = new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z);
            if (currentSkill != null)
            {
                mainAction.performed -= ctx => currentSkill.MainAction();
                secondaryAction.performed -= ctx => currentSkill.SecondaryAction();
                currentSkill.DeactivateSkill();
            }
            currentSkill = skillsList[skillIndex];
            currentSkill.ActivateSkill();
            mainAction.performed += ctx => currentSkill.MainAction();
            secondaryAction.performed += ctx => currentSkill.SecondaryAction();
        }
    }

    private void NextSkill()
    {
        if (currentSkill.IsSkillLocked()) return;
        currentSkillIndex = (currentSkillIndex + 1) % skillsList.Length;
        EquipSkill(currentSkillIndex);
    }

    private void PreviousSkill()
    {
        if (currentSkill.IsSkillLocked()) return;
        currentSkillIndex = (currentSkillIndex - 1 + skillsList.Length) % skillsList.Length;
        EquipSkill(currentSkillIndex);
    }

}
