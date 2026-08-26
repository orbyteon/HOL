using UnityEngine;

/// <summary>
/// Shared asset contract for the approved HOL Cartoon UI v1 language.
/// This catalog owns no hierarchy and performs no late visual writes; each
/// screen keeps exactly one presentation owner and consumes these resources.
/// </summary>
public static class CartoonUiKit
{
    public const string ScreenFrame =
        "dailyhunt/v1/daily_outer_frame_v1";
    public const string TitleRibbon =
        "cartoonui/v1/shared/hol_title_ribbon_v1_raster";
    public const string RewardChest =
        "cartoonui/v1/shared/hol_reward_chest_v1_raster";

    public const string Background =
        "cartoonui/v1/home/hol_home_background_v1";
    public const string Logo = "reference/hol_logo_exact";
    public const string PlayerAvatar = "reference/player_cyan_exact";
    public const string PlayerCharacter = "reference/char_boy_exact";
    public const string FriendCharacter = "reference/char_girl_exact";
    public const string OpponentCharacter = "reference/opponent_purple_exact";
    public const string MascotThree = "reference/mascot_3_exact";
    public const string MascotSix = "reference/mascot_6_exact";
    public const string MascotSeven = "reference/mascot_7_exact";
    public const string Trophy = "cartoonui/v1/raster/hol_trophy_v1";
    public const string VsBurst = "cartoonui/v1/raster/hol_vs_burst_v1";
    public const string Friend = "cartoonui/v1/raster/hol_friend_v1";
    public const string Join = "cartoonui/v1/raster/hol_join_v1";
    public const string Plus = "cartoonui/v1/raster/hol_plus_v1";
    public const string Rocket = "cartoonui/v1/raster/hol_rocket_v1";
    public const string SpeechBubble =
        "cartoonui/v1/raster/hol_speech_bubble_v1";
    public const string RadarBase = "cartoonui/v1/raster/hol_radar_base_v1";
    public const string RadarSweep = "cartoonui/v1/raster/hol_radar_sweep_v1";
    public const string HomeTrophy =
        "cartoonui/v1/home/hol_home_trophy_v1";
    public const string HomeBackground =
        "cartoonui/v1/home/hol_home_background_v1";
    public const string HomeVs = "cartoonui/v1/home/hol_home_vs_v1";
    public const string HomeFriends =
        "cartoonui/v1/home/hol_home_friends_v1";
    public const string HomeTarget =
        "cartoonui/v1/home/hol_home_target_v1";
    public const string HomeGift =
        "cartoonui/v1/home/hol_home_gift_v1";
    public const string PrivateCreateCard =
        "cartoonui/v1/private/hol_private_create_card_v1";
    public const string PrivateJoinCard =
        "cartoonui/v1/private/hol_private_join_card_v1";
    public const string PrivateAddPlayer =
        "cartoonui/v1/private/hol_private_add_player_v1";
    public const string PrivateShare =
        "cartoonui/v1/private/hol_private_share_v1";
    public const string PrivateTipBulb =
        "cartoonui/v1/private/hol_private_tip_bulb_v1";
    public const string DuelPlayerCard =
        "cartoonui/v1/duel/hol_duel_player_card_v1";
    public const string DuelOpponentCard =
        "cartoonui/v1/duel/hol_duel_opponent_card_v1";
    public const string DuelKey =
        "cartoonui/v1/duel/hol_duel_key_v1";
    public const string DuelKeypadBoard =
        "cartoonui/v1/duel/hol_duel_keypad_board_v1";
    public const string DuelBoard =
        "cartoonui/v1/duel/hol_duel_board_v1";
    public const string ResultWinner =
        "cartoonui/v1/result/hol_result_winner_v1";
    public const string BackButton = "dailyhunt/v1/daily_back_button_v1";
    public const string GoldAction = "dailyhunt/v1/daily_action_guess_v1";
    public const string CyanAction = "dailyhunt/v1/daily_action_share_v1";
    public const string PurpleAction = "dailyhunt/v1/daily_action_revive_v1";
    public const string PurpleTrack =
        "dailyhunt/production/daily_player_xp_track_v2";
    public const string PlayerChip =
        "dailyhunt/production/daily_player_chip_shell_v3";
    public const string PlayerAvatarRing =
        "dailyhunt/production/daily_player_avatar_ring_v1";
    public const string PlayerStar =
        "dailyhunt/production/daily_player_star";
    public const string FloorPortal =
        "dailyhunt/production/daily_floor_portal";
    public const string GoldCta = "phase2a/hol_cta_gold_r2_9s";
    public const string CyanCta = "phase2a/hol_cta_blue_r2_9s";
    public const string MagentaCta = "phase2a/hol_cta_magenta_r2_9s";
    public const string PurplePanel = "phase2a/hol_tip_frame_r2_9s";
    public const string DisplayFont = "phase2a/fonts/HOL Menu Display SDF";
    public const string BodyFont = "phase2a/fonts/HOL Menu Body SDF";

    public static readonly Color Ink =
        new Color(0.07f, 0.025f, 0.14f, 1f);
    public static readonly Color NearWhite =
        new Color(0.985f, 0.975f, 1f, 1f);
    public static readonly Color Cyan =
        new Color(0.18f, 0.92f, 1f, 1f);
    public static readonly Color Magenta =
        new Color(1f, 0.20f, 0.64f, 1f);
    public static readonly Color Gold =
        new Color(1f, 0.80f, 0.20f, 1f);
}
