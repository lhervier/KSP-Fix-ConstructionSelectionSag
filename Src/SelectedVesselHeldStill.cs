using System.Collections.Generic;
using UnityEngine;

namespace com.github.lhervier.ksp.fix.constructionselectionsag
{
    /// <summary>
    /// While a part of a landed vessel is selected with the Move or Rotate tool of EVA construction mode, holds every
    /// Rigidbody of that vessel in place (<see cref="RigidbodyConstraints.FreezeAll"/>): the stock code turns the
    /// colliders of the selected part into triggers, so the part no longer bears any weight, and the vessel would sit
    /// down on it. The vessel is let go once the selection ends, the stock code having made the part solid again, or
    /// when a part of another vessel is selected. It goes on rails as landed, even with no ground contact left, since it
    /// is held in place: on stock, a vessel that is not landed follows its orbit through the terrain.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class SelectedVesselHeldStill : MonoBehaviour
    {
        // A part made physical again gets its Rigidbody a frame or more after the change that caused it: the vessel is
        // looked over for new Rigidbodies during these many frames after each change.
        private const int RescanFrames = 5;

        private readonly Dictionary<Rigidbody, RigidbodyConstraints> held = new Dictionary<Rigidbody, RigidbodyConstraints>();
        private Vessel heldVessel;
        private Part lastSelected;
        private int rescanFramesLeft;

        private void Start()
        {
            if (!Settings.HoldVesselStill)
            {
                Destroy(this);
                return;
            }
            GameEvents.onVesselGoOnRails.Add(OnVesselGoOnRails);
            GameEvents.onVesselGoOffRails.Add(OnVesselGoOffRails);
            GameEvents.onVesselWasModified.Add(OnVesselWasModified);
        }

        private void OnDestroy()
        {
            GameEvents.onVesselGoOnRails.Remove(OnVesselGoOnRails);
            GameEvents.onVesselGoOffRails.Remove(OnVesselGoOffRails);
            GameEvents.onVesselWasModified.Remove(OnVesselWasModified);
            LetGo("the flight scene ends");
        }

        // The stock code selects and releases parts in Update: from LateUpdate, the vessel is held before the next
        // physics step, and let go after the released part is solid again.
        private void LateUpdate()
        {
            Part selected = SelectedPart();
            if (selected != lastSelected)
            {
                lastSelected = selected;
                rescanFramesLeft = RescanFrames;
            }
            Vessel wanted = VesselToHold(selected);
            if (wanted != heldVessel)
            {
                LetGo("the selection changed");
                if (wanted != null)
                {
                    Hold(wanted);
                }
            }
            else if (heldVessel != null && rescanFramesLeft > 0 && !heldVessel.packed)
            {
                rescanFramesLeft--;
                Hold(heldVessel);
            }
        }

        // Vessel.GoOnRails fires this before it picks the orbit mode of the vessel: one that is not landed follows its
        // orbit, and is destroyed once below the terrain. The held vessel may have lost its last ground contact (the
        // selected part was its only support, or the player lifted it off the ground), and cannot get it back while
        // held: it stays where it is, as a landed vessel does. The constraints are kept for the return: on rails,
        // Part.Pack makes each Rigidbody kinematic anyway.
        private void OnVesselGoOnRails(Vessel vessel)
        {
            if (vessel != heldVessel || vessel.LandedOrSplashed)
            {
                return;
            }
            vessel.Landed = true;
            Log.Debug($"{vessel.vesselName} held with no ground contact: goes on rails as landed");
        }

        private void OnVesselGoOffRails(Vessel vessel)
        {
            if (vessel == heldVessel)
            {
                rescanFramesLeft = RescanFrames;
            }
        }

        private void OnVesselWasModified(Vessel vessel)
        {
            if (vessel == heldVessel)
            {
                rescanFramesLeft = RescanFrames;
            }
        }

        // The part selected with the Move or Rotate tool, null otherwise.
        private static Part SelectedPart()
        {
            EVAConstructionModeController controller = EVAConstructionModeController.Instance;
            if (controller == null || !controller.IsOpen || controller.panelMode != EVAConstructionModeController.PanelMode.Construction)
            {
                return null;
            }
            EVAConstructionModeEditor editor = controller.evaEditor;
            if (editor == null)
            {
                return null;
            }
            ConstructionMode mode = editor.EVAConstructionMode;
            if (mode != ConstructionMode.Move && mode != ConstructionMode.Rotate)
            {
                return null;
            }
            return editor.SelectedPart;
        }

        // The vessel to hold for this selected part: its own, if it is landed when the hold starts. Once held, it stays
        // held while its part is selected, on rails or not, and whatever its ground contacts become.
        private Vessel VesselToHold(Part selected)
        {
            if (selected == null || selected.vessel == null)
            {
                return null;
            }
            Vessel vessel = selected.vessel;
            if (vessel == heldVessel)
            {
                return vessel;
            }
            if (vessel.packed || !vessel.LandedOrSplashed)
            {
                return null;
            }
            return vessel;
        }

        // Freezes the Rigidbodies of the vessel not held yet, keeping their constraints to give them back.
        private void Hold(Vessel vessel)
        {
            int added = 0;
            for (int i = 0; i < vessel.parts.Count; i++)
            {
                Rigidbody rb = vessel.parts[i].rb;
                if (rb == null || held.ContainsKey(rb))
                {
                    continue;
                }
                held[rb] = rb.constraints;
                rb.constraints = RigidbodyConstraints.FreezeAll;
                added++;
            }
            heldVessel = vessel;
            if (added > 0)
            {
                Log.Debug($"Holding {added} Rigidbody(ies) of {vessel.vesselName}, {held.Count} in all");
            }
        }

        // Gives the held Rigidbodies their constraints back.
        private void LetGo(string why)
        {
            if (heldVessel == null && held.Count == 0)
            {
                return;
            }
            int released = 0;
            foreach (KeyValuePair<Rigidbody, RigidbodyConstraints> entry in held)
            {
                if (entry.Key == null)
                {
                    continue;
                }
                entry.Key.constraints = entry.Value;
                released++;
            }
            Log.Debug($"Letting go {released} Rigidbody(ies) of {(heldVessel != null ? heldVessel.vesselName : "a vessel gone")}: {why}");
            held.Clear();
            heldVessel = null;
        }
    }
}
