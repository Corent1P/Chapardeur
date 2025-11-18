using UnityEngine;

public interface ISkills
{
    ISkills ActivateSkill();
    ISkills DeactivateSkill();

    void ChangeAppearance();
    void MainAction();
    void SecondaryAction();
}