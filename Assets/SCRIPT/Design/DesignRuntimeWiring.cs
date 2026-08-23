using UnityEngine;

// TEMPORARY SERIALIZATION COMPATIBILITY SHIM.
//
// MainMenu.unity still serializes this MonoBehaviour while the legacy-theme
// purge removes that scene component in a controlled YAML/Unity pass. The old
// implementation globally recolored panels/buttons and generated background
// overlays, which violated the production asset-fidelity and one-screen/
// one-owner contracts.
//
// This class intentionally performs NO runtime work and exposes NO shared theme
// state. Delete this file and its .meta immediately after the serialized scene
// component has been removed, so the project never ships a Missing Script.
[DisallowMultipleComponent]
public sealed class DesignRuntimeWiring : MonoBehaviour
{
}
