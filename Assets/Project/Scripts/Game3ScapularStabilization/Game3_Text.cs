/// <summary>
/// 射击行动 - 文本常量
/// </summary>
public static class Game3_Text
{
    // 动作提示
    public const string HintSitUpright = "请坐正，双臂自然下垂，掌心朝内";
    public const string HintRelaxArms = "请放松双臂，保持自然下垂姿势";
    public const string HintCalibrating = "保持起始姿势，正在校准";
    public const string HintCalibrationFailed = "校准失败，请检查控制器后重试";
    public const string HintRetractShoulders = "缓慢向后收紧肩胛骨，准备射击";
    public const string HintKeepRetracting = "很好，继续保持肩胛后缩";
    public const string HintHoldSteady = "保持稳定，不要抖动";
    public const string HintReturnStart = "很好，慢慢放松回到起始位置";
    public const string HintAdjustPosture = "动作还不够标准，请调整姿势";
    public const string HintControllerMissing = "未找到控制器，请检查双手控制器引用";
    public const string HintRetractionTooDeep = "收缩过度，请回到舒适范围";
    public const string HintActionTooLong = "动作时间过长，请调整后重新开始";
    public const string HintKeepElbowsClose = "请保持手臂贴近身体";
    public const string HintDoNotShrug = "请不要耸肩，保持肩部下沉";
    public const string HintSlowDown = "动作可以再慢一点";
    public const string HintFaceTarget = "请保持面向目标";
    public const string HintCurrentWarning = "检测到抖动，请保持稳定";

    // 射击反馈
    public const string ShootExcellent = "完美射击!";
    public const string ShootGood = "精准射击!";
    public const string ShootImprove = "勉强命中";
    public const string ShootMiss = "动作不标准 - 未发射";
    public const string ShootStart = "射击开始!";
    public const string ShootCountdown = "准备...";

    // HUD标签
    public const string LabelScore = "得分";
    public const string LabelTime = "时间";
    public const string LabelCombo = "连击";
    public const string LabelHits = "命中";

    // 评级
    public const string GradeS = "S级 - 神射手!";
    public const string GradeA = "A级 - 精准射击!";
    public const string GradeB = "B级 - 继续加油!";
    public const string GradeC = "C级 - 需要练习";
    public const string GradeD = "D级 - 再接再厉";
    public const string GradeExcellent = "优秀";
    public const string GradeGood = "良好";
    public const string GradeNeedsImprovement = "需改进";
    public const string GradeInvalid = "动作无效";

    // 结果
    public const string ResultTitle = "射击完成";
    public const string ResultFinalScore = "最终得分";
    public const string ResultHits = "命中次数";
    public const string ResultBestCombo = "最高连击";
    public const string ResultRestart = "再来一局";
    public const string ResultMenu = "返回菜单";

    // 场景
    public const string SceneIntro = "射击行动";
    public const string SceneDescription = "通过肩胛稳定训练，完成精准射击";
}
