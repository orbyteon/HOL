# Protected-player behavior in the current age bands

The approved onboarding categories remain under 13, 13 to 17 and 18+. No additional personal data is collected. The 13 to 17 band cannot implement an exact under-15 threshold, so the proposed conservative default protects both younger bands and missing/invalid legacy ages.

Only a committed explicit adult choice enables ad SDK initialization, loads, shows, rewarded readiness and the native external-app invitation chooser. Consent remains independently required. Protected profiles do not see the ad consent prompt or native share button. The game and deliberate private-room code copying remain available, including the existing copy mission; there is no new free-text chat or direct contact exchange. This is a restricted external-app launch policy, not a claim that copying a room code prevents subsequent off-app sharing.

Unknown ages no longer deserialize as an adult choice. Legacy accounts continue to play without being reset or forced through onboarding, with ads and the chooser disabled until a valid age is recorded through an approved profile flow. A future finer age policy and legacy age-update UX require product review; this change does not invent a player's age.

Native visual acceptance and ad-provider configuration are separate release checks. The code does not constitute a legal compliance determination.
