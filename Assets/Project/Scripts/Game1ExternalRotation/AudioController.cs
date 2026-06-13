public class Game1_AudioController : TrainingAudioController
{
    public void PlayActionResult(Game1_ActionGrade grade)
    {
        switch (grade)
        {
            case Game1_ActionGrade.Excellent:
                PlayExcellent();
                break;
            case Game1_ActionGrade.Good:
                PlayGood();
                break;
            case Game1_ActionGrade.NeedsImprovement:
                PlayNeedsImprovement();
                break;
            default:
                PlayInvalid();
                break;
        }
    }
}
