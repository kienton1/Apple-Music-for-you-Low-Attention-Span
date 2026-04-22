using AutoAppleMusic.Core;

namespace AutoAppleMusic.Tests;

public sealed class AutomationScenarioTests
{
    [Fact]
    public void Disabling_automation_clears_pending_work_and_reenabling_applies_the_latest_state()
    {
        var stateMachine = CreateEnabledStateMachine();

        var pauseDecision = stateMachine.Observe(Observation(externalAudio: true, appleMusicAvailable: true, PlaybackState.Playing));
        AssertActions(pauseDecision, DesiredAction.PauseAppleMusic);

        var disableDecision = stateMachine.SetEnabled(false);
        AssertActions(disableDecision);
        Assert.Null(disableDecision.Snapshot.PendingAction);
        Assert.False(disableDecision.Snapshot.IsEnabled);

        var whileDisabled = stateMachine.Observe(Observation(externalAudio: false, appleMusicAvailable: true, PlaybackState.Paused));
        AssertActions(whileDisabled);

        var reenableDecision = stateMachine.SetEnabled(true);
        AssertActions(reenableDecision, DesiredAction.PlayAppleMusic);
        Assert.Equal(DesiredAction.PlayAppleMusic, reenableDecision.Snapshot.PendingAction);
        Assert.True(reenableDecision.Snapshot.IsEnabled);
    }

    [Fact]
    public void External_audio_start_pauses_apple_music_and_external_audio_end_resumes_it()
    {
        var stateMachine = CreateEnabledStateMachine();

        var pauseDecision = stateMachine.Observe(Observation(externalAudio: true, appleMusicAvailable: true, PlaybackState.Playing));
        AssertActions(pauseDecision, DesiredAction.PauseAppleMusic);
        Assert.Equal(DesiredAction.PauseAppleMusic, pauseDecision.Snapshot.PendingAction);

        var pauseObserved = stateMachine.Observe(Observation(externalAudio: true, appleMusicAvailable: true, PlaybackState.Paused));
        AssertActions(pauseObserved);
        Assert.Null(pauseObserved.Snapshot.PendingAction);

        var resumeDecision = stateMachine.Observe(Observation(externalAudio: false, appleMusicAvailable: true, PlaybackState.Paused));
        AssertActions(resumeDecision, DesiredAction.PlayAppleMusic);
        Assert.Equal(DesiredAction.PlayAppleMusic, resumeDecision.Snapshot.PendingAction);
    }

    [Fact]
    public void Manual_pause_blocks_auto_resume_until_a_fresh_external_audio_cycle_finishes()
    {
        var stateMachine = CreateEnabledStateMachine();

        AssertActions(
            stateMachine.Observe(Observation(externalAudio: true, appleMusicAvailable: true, PlaybackState.Playing)),
            DesiredAction.PauseAppleMusic);
        AssertActions(stateMachine.Observe(Observation(externalAudio: true, appleMusicAvailable: true, PlaybackState.Paused)));
        AssertActions(
            stateMachine.Observe(Observation(externalAudio: false, appleMusicAvailable: true, PlaybackState.Paused)),
            DesiredAction.PlayAppleMusic);
        AssertActions(stateMachine.Observe(Observation(externalAudio: false, appleMusicAvailable: true, PlaybackState.Playing)));

        var manualPauseDecision = stateMachine.Observe(Observation(externalAudio: false, appleMusicAvailable: true, PlaybackState.Paused));
        AssertActions(manualPauseDecision);
        Assert.Contains(AutomationEventKind.AutoResumeBlockedByManualPause, manualPauseDecision.Events);
        Assert.False(manualPauseDecision.Snapshot.CanAutoResume);

        var duringFreshCycle = stateMachine.Observe(Observation(externalAudio: true, appleMusicAvailable: true, PlaybackState.Paused));
        AssertActions(duringFreshCycle);
        Assert.False(duringFreshCycle.Snapshot.CanAutoResume);

        var cycleCompleted = stateMachine.Observe(Observation(externalAudio: false, appleMusicAvailable: true, PlaybackState.Paused));
        AssertActions(cycleCompleted, DesiredAction.PlayAppleMusic);
        Assert.Contains(AutomationEventKind.AutoResumeReenabled, cycleCompleted.Events);
        Assert.True(cycleCompleted.Snapshot.CanAutoResume);
    }

    [Fact]
    public void Resume_requests_launch_when_apple_music_is_unavailable()
    {
        var stateMachine = CreateEnabledStateMachine();

        AssertActions(
            stateMachine.Observe(Observation(externalAudio: true, appleMusicAvailable: true, PlaybackState.Playing)),
            DesiredAction.PauseAppleMusic);
        AssertActions(stateMachine.Observe(Observation(externalAudio: true, appleMusicAvailable: true, PlaybackState.Paused)));

        var launchDecision = stateMachine.Observe(Observation(externalAudio: false, appleMusicAvailable: false, PlaybackState.Paused));
        AssertActions(launchDecision, DesiredAction.LaunchAppleMusic);
        Assert.Equal(DesiredAction.LaunchAppleMusic, launchDecision.Snapshot.PendingAction);

        var playDecision = stateMachine.Observe(Observation(externalAudio: false, appleMusicAvailable: true, PlaybackState.Paused));
        AssertActions(playDecision, DesiredAction.PlayAppleMusic);
        Assert.Equal(DesiredAction.PlayAppleMusic, playDecision.Snapshot.PendingAction);
    }

    [Fact]
    public void Repeated_state_reports_do_not_emit_duplicate_pause_or_resume_commands()
    {
        var stateMachine = CreateEnabledStateMachine();

        var firstPause = stateMachine.Observe(Observation(externalAudio: true, appleMusicAvailable: true, PlaybackState.Playing));
        AssertActions(firstPause, DesiredAction.PauseAppleMusic);

        var duplicatePause = stateMachine.Observe(Observation(externalAudio: true, appleMusicAvailable: true, PlaybackState.Playing));
        AssertActions(duplicatePause);
        Assert.Equal(DesiredAction.PauseAppleMusic, duplicatePause.Snapshot.PendingAction);

        AssertActions(stateMachine.Observe(Observation(externalAudio: true, appleMusicAvailable: true, PlaybackState.Paused)));

        var firstResume = stateMachine.Observe(Observation(externalAudio: false, appleMusicAvailable: true, PlaybackState.Paused));
        AssertActions(firstResume, DesiredAction.PlayAppleMusic);

        var duplicateResume = stateMachine.Observe(Observation(externalAudio: false, appleMusicAvailable: true, PlaybackState.Paused));
        AssertActions(duplicateResume);
        Assert.Equal(DesiredAction.PlayAppleMusic, duplicateResume.Snapshot.PendingAction);
    }

    private static AutomationStateMachine CreateEnabledStateMachine()
    {
        var stateMachine = new AutomationStateMachine();

        AssertActions(stateMachine.Observe(Observation(externalAudio: false, appleMusicAvailable: true, PlaybackState.Playing)));
        AssertActions(stateMachine.SetEnabled(true));

        return stateMachine;
    }

    private static AutomationObservation Observation(
        bool externalAudio,
        bool appleMusicAvailable,
        PlaybackState playbackState) =>
        new(externalAudio, appleMusicAvailable, playbackState);

    private static void AssertActions(
        AutomationDecision decision,
        params DesiredAction[] expected)
    {
        Assert.Equal(expected, decision.Actions.ToArray());
    }
}
