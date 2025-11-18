using UnityEngine;

public class SkillManager : MonoBehaviour
{
    [SerializeField] private int currentSkillIndex = 0;
    [SerializeField] private ASkills[] skillsList;
    private PlayerInputs inputActions;
    private ASkills currentSkill;

    private void Awake()
    {
        inputActions = new PlayerInputs();
    }

    private void OnEnable()
    {
        inputActions.PlayerControls.Enable();

        inputActions.PlayerControls.NextSkill.performed += ctx => NextSkill();
        inputActions.PlayerControls.PreviousSkill.performed += ctx => PreviousSkill();
        EquipSkill(currentSkillIndex = 0);
    }

    private void OnDisable()
    {
        inputActions.PlayerControls.Disable();

        inputActions.PlayerControls.NextSkill.performed -= ctx => NextSkill();
        inputActions.PlayerControls.PreviousSkill.performed -= ctx => PreviousSkill();
        if (currentSkill != null)
        {
            inputActions.PlayerControls.MainAction.performed -= ctx => currentSkill.MainAction();
            inputActions.PlayerControls.SecondaryAction.performed -= ctx => currentSkill.SecondaryAction();
        }
    }


    private void EquipSkill(int skillIndex)
    {
        if (skillIndex >= 0 && skillIndex < skillsList.Length)
        {
            transform.position = new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z);
            if (currentSkill != null)
            {
                inputActions.PlayerControls.MainAction.performed -= ctx => currentSkill.MainAction();
                inputActions.PlayerControls.SecondaryAction.performed -= ctx => currentSkill.SecondaryAction();
                currentSkill.DeactivateSkill();
            }
            currentSkill = skillsList[skillIndex];
            currentSkill.ActivateSkill();
            inputActions.PlayerControls.MainAction.performed += ctx => currentSkill.MainAction();
            inputActions.PlayerControls.SecondaryAction.performed += ctx => currentSkill.SecondaryAction();
        }
    }

    private void NextSkill()
    {
        currentSkillIndex = (currentSkillIndex + 1) % skillsList.Length;
        EquipSkill(currentSkillIndex);
    }

    private void PreviousSkill()
    {
        currentSkillIndex = (currentSkillIndex - 1 + skillsList.Length) % skillsList.Length;
        EquipSkill(currentSkillIndex);
    }

}
