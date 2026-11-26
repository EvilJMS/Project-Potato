using System.Collections.Generic;
using UnityEngine;

public class RaceTrackBuilder : MonoBehaviour
{
    [Header("Dimensiones Generales (Largo y Muy Ancho)")]
    [SerializeField] private float trackWidth = 38f;
    [SerializeField] private float startLength = 28f;
    [SerializeField] private float rampLength = 16f;
    [SerializeField] private float rampHeight = 5f;
    [SerializeField] private float finishLength = 32f;

    [Header("Seguridad de Bordes (PushBlocks y Checkpoints)")]
    [SerializeField] private float pushBlockSize = 3.2f;
    [SerializeField] private float edgeMargin = 2.0f;

    [Header("Spawn Points")]
    [SerializeField] private int spawnCount = 24;
    [SerializeField] private float spawnSpacingX = 2.5f;
    [SerializeField] private float spawnSpacingZ = 3.2f;

    [Header("Materiales opcionales")]
    [SerializeField] private Material floorMaterial;
    [SerializeField] private Material checkpointMaterial;
    [SerializeField] private Material obstacleMaterial;
    [SerializeField] private Material finishMaterial;

    private const string GeneratedRootName = "Generated_FallGuysTrack_Final";

    [ContextMenu("Generar Circuito Fall Guys Completo")]
    public void GenerateTrack()
    {
        ClearGeneratedTrack();

        GameObject root = new GameObject(GeneratedRootName);
        root.transform.SetParent(transform, false);

        float z = 0f;
        float y = 0f;
        int checkpointIndex = 1;

        // 01. SALIDA MUY ANCHA (PARRILLA DE SPAWNS)
        GameObject startZone = CreateFolder("01_StartZone", root.transform);
        CreateBox("StartFloor", startZone.transform,
            new Vector3(0f, y - 0.5f, z + startLength * 0.5f),
            new Vector3(trackWidth, 1f, startLength),
            floorMaterial, "Ground");

        CreateSpawnPoints(startZone.transform, y + 0.5f, z + 5f);
        z += startLength;

        // 02. SECCIÓN 1: RAMPA DE SUBIDA + MESETA + CHECKPOINT 1 (EN EL CENTRO DE LA MESETA)
        GameObject sec1 = CreateFolder("02_Section_RampAndCheckpoint", root.transform);
        CreateRamp("Ramp_Ascent", sec1.transform,
            new Vector3(0f, y, z), trackWidth, rampLength, rampHeight,
            floorMaterial, "Ground");
        z += rampLength;
        y += rampHeight;

        float plateauLen = 18f;
        CreateBox("Plateau_1", sec1.transform,
            new Vector3(0f, y - 0.5f, z + plateauLen * 0.5f),
            new Vector3(trackWidth, 1f, plateauLen),
            floorMaterial, "Ground");

        CreateCheckpoint(sec1.transform, checkpointIndex++,
            new Vector3(0f, y, z + plateauLen * 0.55f),
            trackWidth, respawnOffsetZ: -2.0f);
        z += plateauLen;

        // 03. SECCIÓN 2: SERIE DE 4 SALTOS ANCHOS + CHECKPOINT 2
        GameObject sec2 = CreateFolder("03_Section_JumpGaps", root.transform);
        float gap = 3.2f;
        float padLen = 11f;

        for (int i = 0; i < 4; i++)
        {
            float widthFactor = 1f - (i * 0.04f); // 100%, 96%, 92%, 88% de trackWidth
            CreateBox($"JumpPad_{i + 1}", sec2.transform,
                new Vector3(0f, y - 0.5f, z + gap + padLen * 0.5f),
                new Vector3(trackWidth * widthFactor, 1f, padLen),
                floorMaterial, "Ground");
            z += gap + padLen;
        }

        CreateCheckpoint(sec2.transform, checkpointIndex++,
            new Vector3(0f, y, z - padLen * 0.45f),
            trackWidth * 0.88f, respawnOffsetZ: -1.5f);

        // 04. SECCIÓN 3: PISTA LARGA DE 4 SWEEPERS + CHECKPOINT 3
        GameObject sec3 = CreateFolder("04_Section_Sweepers", root.transform);
        float sweeperLen = 50f;
        CreateBox("SweeperFloor", sec3.transform,
            new Vector3(0f, y - 0.5f, z + sweeperLen * 0.5f),
            new Vector3(trackWidth, 1f, sweeperLen),
            floorMaterial, "Ground");

        float[] sweeperOffsets = { 8f, 19f, 30f, 41f };
        Vector3[] sweeperSpeeds =
        {
            new Vector3(0f, 80f, 0f),
            new Vector3(0f, -105f, 0f),
            new Vector3(0f, 125f, 0f),
            new Vector3(0f, -140f, 0f)
        };

        for (int i = 0; i < sweeperOffsets.Length; i++)
        {
            CreateSweeper(sec3.transform,
                new Vector3(0f, y + 0.65f, z + sweeperOffsets[i]),
                trackWidth * 0.9f,
                sweeperSpeeds[i % sweeperSpeeds.Length],
                obstacleMaterial);
        }

        CreateCheckpoint(sec3.transform, checkpointIndex++,
            new Vector3(0f, y, z + sweeperLen - 4f),
            trackWidth, respawnOffsetZ: -2.0f);
        z += sweeperLen;

        // 05. SECCIÓN 4: 4 PLATAFORMAS MÓVILES ANCHAS + CHECKPOINT 4
        GameObject sec4 = CreateFolder("05_Section_MovingPlatforms", root.transform);
        CreateBox("MovingStartLedge", sec4.transform,
            new Vector3(0f, y - 0.5f, z + 3.5f),
            new Vector3(trackWidth, 1f, 7f),
            floorMaterial, "Ground");
        z += 7f;

        float platWidth = Mathf.Max(8f, trackWidth * 0.34f);
        float platLength = 7f;
        float platStepZ = 10f;
        float[] xPositions =
        {
            -trackWidth * 0.18f,
            trackWidth * 0.18f,
            -trackWidth * 0.12f,
            trackWidth * 0.12f
        };

        for (int i = 0; i < xPositions.Length; i++)
        {
            CreateMovingPlatform(sec4.transform,
                new Vector3(xPositions[i], y - 0.5f, z + platLength * 0.5f),
                new Vector3(platWidth, 1f, platLength),
                floorMaterial, "Ground");
            z += platStepZ;
        }

        float endPlatLen = 9f;
        CreateBox("MovingEndPlatform", sec4.transform,
            new Vector3(0f, y - 0.5f, z + endPlatLen * 0.5f),
            new Vector3(trackWidth, 1f, endPlatLen),
            floorMaterial, "Ground");

        CreateCheckpoint(sec4.transform, checkpointIndex++,
            new Vector3(0f, y, z + endPlatLen * 0.6f),
            trackWidth, respawnOffsetZ: -1.5f);
        z += endPlatLen;

        // 06. SECCIÓN 5: CAMPO LARGO DE BLOQUES DE EMPUJE (SIEMPRE DENTRO DEL SUELO) + CHECKPOINT 5
        GameObject sec5 = CreateFolder("06_Section_BumpersField", root.transform);
        float bumperLen = 42f;
        CreateBox("BumperFloor", sec5.transform,
            new Vector3(0f, y - 0.5f, z + bumperLen * 0.5f),
            new Vector3(trackWidth, 1f, bumperLen),
            floorMaterial, "Ground");

        Vector3 blockScale = new Vector3(pushBlockSize, 2.6f, pushBlockSize);
        float blockY = y + blockScale.y * 0.5f;

        float[] rowOffsets = { 6f, 13f, 20f, 27f, 34f };
        for (int r = 0; r < rowOffsets.Length; r++)
        {
            int blocksInRow = (r % 2 == 0) ? 4 : 3;
            float rowZ = z + rowOffsets[r];

            for (int c = 0; c < blocksInRow; c++)
            {
                float t = blocksInRow == 1 ? 0.5f : (float)c / (blocksInRow - 1);
                float clampedX = GetSafeX(t, trackWidth, pushBlockSize, edgeMargin);
                CreatePushBlock(sec5.transform, new Vector3(clampedX, blockY, rowZ), blockScale, obstacleMaterial);
            }
        }

        CreateCheckpoint(sec5.transform, checkpointIndex++,
            new Vector3(0f, y, z + bumperLen - 3.5f),
            trackWidth, respawnOffsetZ: -2.0f);
        z += bumperLen;

        // 07. SECCIÓN 6: BAJADA + OBSTÁCULOS FINALES + META
        GameObject finishZone = CreateFolder("07_DescentAndFinish", root.transform);
        CreateRamp("Ramp_Descent", finishZone.transform,
            new Vector3(0f, y, z), trackWidth, rampLength, -rampHeight,
            floorMaterial, "Ground");
        z += rampLength;
        y -= rampHeight;

        CreateBox("FinalFloor", finishZone.transform,
            new Vector3(0f, y - 0.5f, z + finishLength * 0.5f),
            new Vector3(trackWidth, 1f, finishLength),
            floorMaterial, "Ground");

        CreateSweeper(finishZone.transform,
            new Vector3(0f, y + 0.65f, z + finishLength * 0.32f),
            trackWidth * 0.9f,
            new Vector3(0f, 135f, 0f),
            obstacleMaterial);

        CreateSweeper(finishZone.transform,
            new Vector3(0f, y + 0.65f, z + finishLength * 0.62f),
            trackWidth * 0.9f,
            new Vector3(0f, -155f, 0f),
            obstacleMaterial);

        CreateFinishLine(finishZone.transform,
            new Vector3(0f, y + 2.2f, z + finishLength - 3f),
            new Vector3(trackWidth, 4.4f, 2f),
            finishMaterial);

        z += finishLength;

        // 08. KILLPLANE GENERAL
        GameObject hazardZone = CreateFolder("08_HazardZone", root.transform);
        GameObject killPlane = new GameObject("KillPlane");
        killPlane.transform.SetParent(hazardZone.transform, false);
        killPlane.transform.position = new Vector3(0f, y - 18f, z * 0.5f);

        BoxCollider killCol = killPlane.AddComponent<BoxCollider>();
        killCol.isTrigger = true;
        killCol.size = new Vector3(trackWidth * 5f, 1f, z + 60f);
        killPlane.AddComponent<KillPlaneTrigger>();
    }

    private float GetSafeX(float normalizedT, float width, float objWidth, float margin)
    {
        float halfTrack = width * 0.5f;
        float halfObj = objWidth * 0.5f;
        float maxAbsX = Mathf.Max(0f, halfTrack - halfObj - margin);
        return Mathf.Lerp(-maxAbsX, maxAbsX, Mathf.Clamp01(normalizedT));
    }

    [ContextMenu("Limpiar Circuito Generado")]
    public void ClearGeneratedTrack()
    {
        string[] oldNames =
        {
            GeneratedRootName,
            "Generated_FallGuysTrack_XXL",
            "Generated_FallGuysTrack_XL",
            "Generated_FallGuysTrack_Wide",
            "Generated_FallGuysTrack"
        };

        foreach (string n in oldNames)
        {
            Transform existing = transform.Find(n);
            if (existing != null)
            {
                if (Application.isPlaying) Destroy(existing.gameObject);
                else DestroyImmediate(existing.gameObject);
            }
        }
    }

    private GameObject CreateFolder(string folderName, Transform parent)
    {
        GameObject obj = new GameObject(folderName);
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private void CreateBox(string objName, Transform parent, Vector3 localPos, Vector3 scale, Material mat, string tagName)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = objName;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = localPos;
        cube.transform.localScale = scale;

        TrySetTag(cube, tagName);
        ApplyMaterial(cube, mat);
    }

    private void CreateRamp(string objName, Transform parent, Vector3 startPos, float width, float length, float height, Material mat, string tagName)
    {
        GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ramp.name = objName;
        ramp.transform.SetParent(parent, false);

        float slopeLength = Mathf.Sqrt(length * length + height * height);
        float thickness = 0.6f;
        float angleRad = Mathf.Atan2(-height, length);
        float angleDeg = angleRad * Mathf.Rad2Deg;

        Vector3 midPoint = startPos + new Vector3(0f, height * 0.5f, length * 0.5f);
        Vector3 normalOffset = new Vector3(0f, -thickness * 0.5f / Mathf.Max(0.2f, Mathf.Cos(angleRad)), 0f);

        ramp.transform.localPosition = midPoint + normalOffset;
        ramp.transform.localRotation = Quaternion.Euler(angleDeg, 0f, 0f);
        ramp.transform.localScale = new Vector3(width, thickness, slopeLength);

        TrySetTag(ramp, tagName);
        ApplyMaterial(ramp, mat);
    }

    private void CreateSpawnPoints(Transform parent, float yPos, float zPos)
    {
        GameObject containerObj = new GameObject("SpawnPoints");
        containerObj.transform.SetParent(parent, false);
        containerObj.transform.localPosition = new Vector3(0f, yPos, zPos);

        SceneSpawnPoints spawnComp = containerObj.AddComponent<SceneSpawnPoints>();
        List<Transform> points = new List<Transform>();

        int cols = Mathf.Max(1, Mathf.FloorToInt((trackWidth - 4f) / spawnSpacingX));
        for (int i = 0; i < spawnCount; i++)
        {
            int col = i % cols;
            int row = i / cols;

            float xOffset = (col - (cols - 1) * 0.5f) * spawnSpacingX;
            float zOffset = -row * spawnSpacingZ;

            GameObject sp = new GameObject($"Spawn_{i}");
            sp.transform.SetParent(containerObj.transform, false);
            sp.transform.localPosition = new Vector3(xOffset, 0f, zOffset);
            points.Add(sp.transform);
        }

        spawnComp.spawnPoints = points.ToArray();
    }

    private void CreateCheckpoint(Transform parent, int index, Vector3 floorPos, float width, float respawnOffsetZ = -1.5f)
    {
        GameObject cpObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cpObj.name = $"Checkpoint_{index}";
        cpObj.transform.SetParent(parent, false);
        cpObj.transform.localPosition = floorPos + new Vector3(0f, 1.8f, 0f);
        cpObj.transform.localScale = new Vector3(width, 3.6f, 1.4f);

        BoxCollider col = cpObj.GetComponent<BoxCollider>();
        col.isTrigger = true;

        // RespawnPoint colgado directamente de parent (sin escala heredada de cpObj)
        GameObject respawnChild = new GameObject($"RespawnPoint_{index}");
        respawnChild.transform.SetParent(parent, false);
        respawnChild.transform.localPosition = floorPos + new Vector3(0f, 0.8f, respawnOffsetZ);

        if (Physics.Raycast(respawnChild.transform.position + Vector3.up * 4f, Vector3.down, out RaycastHit hit, 12f))
        {
            respawnChild.transform.position = hit.point + Vector3.up * 0.5f;
        }

        CheckpointTrigger cpTrigger = cpObj.AddComponent<CheckpointTrigger>();
#if UNITY_EDITOR
        UnityEditor.SerializedObject so = new UnityEditor.SerializedObject(cpTrigger);
        UnityEditor.SerializedProperty prop = so.FindProperty("respawnPoint");
        if (prop != null)
        {
            prop.objectReferenceValue = respawnChild.transform;
            so.ApplyModifiedProperties();
        }
#endif

        Renderer rend = cpObj.GetComponent<Renderer>();
        if (rend != null)
        {
            if (checkpointMaterial != null) rend.sharedMaterial = checkpointMaterial;
            else rend.sharedMaterial.color = new Color(0.2f, 0.7f, 1f, 0.35f);
        }
    }

    private void CreateSweeper(Transform parent, Vector3 centerPos, float barLength, Vector3 eulerSpeed, Material mat)
    {
        GameObject pivot = new GameObject("SweeperPivot");
        pivot.transform.SetParent(parent, false);
        pivot.transform.localPosition = centerPos;

        GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bar.name = "SweeperBar";
        bar.transform.SetParent(pivot.transform, false);
        bar.transform.localPosition = Vector3.zero;
        bar.transform.localScale = new Vector3(barLength, 0.75f, 0.75f);

        Rigidbody rb = bar.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        ApplyMaterial(bar, mat);
        bar.AddComponent<PushObstacle>();

        RotatingObstacle rot = pivot.AddComponent<RotatingObstacle>();
        rot.SetRotationSpeed(eulerSpeed);
    }

    private void CreateMovingPlatform(Transform parent, Vector3 localPos, Vector3 scale, Material mat, string tagName)
    {
        GameObject plat = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plat.name = "MovingPlatform";
        plat.transform.SetParent(parent, false);
        plat.transform.localPosition = localPos;
        plat.transform.localScale = scale;

        TrySetTag(plat, tagName);
        ApplyMaterial(plat, mat);
        plat.AddComponent<MovingPlatformObstacle>();
    }

    private void CreatePushBlock(Transform parent, Vector3 localPos, Vector3 scale, Material mat)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = "PushBlock";
        block.transform.SetParent(parent, false);
        block.transform.localPosition = localPos;
        block.transform.localScale = scale;

        Rigidbody rb = block.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        ApplyMaterial(block, mat);
        block.AddComponent<PushObstacle>();
    }

    private void CreateFinishLine(Transform parent, Vector3 localPos, Vector3 size, Material mat)
    {
        GameObject finishObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        finishObj.name = "FinishLineTrigger";
        finishObj.transform.SetParent(parent, false);
        finishObj.transform.localPosition = localPos;
        finishObj.transform.localScale = size;

        BoxCollider col = finishObj.GetComponent<BoxCollider>();
        col.isTrigger = true;

        Renderer rend = finishObj.GetComponent<Renderer>();
        if (rend != null)
        {
            if (finishMaterial != null) rend.sharedMaterial = finishMaterial;
            else rend.sharedMaterial.color = new Color(0.2f, 1f, 0.4f, 0.35f);
        }

        finishObj.AddComponent<FinishLineTrigger>();
    }

    private void TrySetTag(GameObject obj, string tagName)
    {
        if (string.IsNullOrEmpty(tagName)) return;
        try
        {
            obj.tag = tagName;
        }
        catch (UnityException)
        {
            Debug.LogWarning($"[RaceTrackBuilder] El tag '{tagName}' no existe en Tag Manager.");
        }
    }

    private void ApplyMaterial(GameObject obj, Material mat)
    {
        if (mat == null) return;
        Renderer rend = obj.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.sharedMaterial = mat;
        }
    }
}