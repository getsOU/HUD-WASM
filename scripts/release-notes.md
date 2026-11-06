# 0.4.23

- Add spaced, team-colored kill symbols for the current round outside playercards, without numbers, backgrounds or frames. Align side-layout indicators with the card's top edge and place horizontal-layout indicators above the card. Retain kills after death, hide them during freeze/warmup, and animate newly added symbols with reduced-motion support.
- Position the competition logo at the right edge with rotating sponsors directly to its left. Size both by their panel-controlled height and original aspect ratio, without fixed-width slots.
- Respect configured delayed match starts in prematch and break countdowns, label the delay and show the original schedule when known. Restore the regular schedule when a delay is cleared and support delayed-only or unknown start times.
- Hide the complete start display when its effective target is reached; show it again when a new future start is supplied.
- Display all planned and delayed start dates in the browser/OBS local timezone, including daylight saving time, without visible timezone suffixes. Keep countdowns on the SDK synchronized clock.
- Update the public SDK, Blazor adapter, contracts and HUD tool to 0.3.0-preview.3. Delayed-start data requires sOU Desktop 0.3.38 or newer; older senders retain the regular-start fallback. Select version 0.4.23 and reopen outputs after importing.
