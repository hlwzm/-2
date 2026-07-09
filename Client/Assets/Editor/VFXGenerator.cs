using UnityEngine;
using UnityEditor;

public class VFXGenerator : EditorWindow
{
    [MenuItem("Jx3/VFX/Generate All VFX")]
    static void GenerateAllVFX()
    {
        string outputDir = "Assets/Art/Effects/Skills";
        if (!AssetDatabase.IsValidFolder(outputDir))
        {
            string parent = "Assets/Art/Effects";
            if (!AssetDatabase.IsValidFolder(parent))
                AssetDatabase.CreateFolder("Assets/Art", "Effects");
            AssetDatabase.CreateFolder(parent, "Skills");
        }

        CreateSkillFire(outputDir);
        CreateSkillIce(outputDir);
        CreateSkillLightning(outputDir);
        CreateSkillHeal(outputDir);
        CreateSkillWind(outputDir);
        CreateBuffAttack(outputDir);
        CreateBuffDefense(outputDir);
        CreateHitSmall(outputDir);
        CreateHitBig(outputDir);

        AssetDatabase.Refresh();
        Debug.Log("[VFXGenerator] All 9 VFX prefabs generated to " + outputDir);
    }

    static GameObject CreateBaseEffect(string name, Color color, float duration = 1f)
    {
        GameObject go = new GameObject(name);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startColor = color;
        main.startSize = 0.5f;
        main.startLifetime = duration;
        main.duration = duration;
        main.loop = false;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        return go;
    }

    static void SavePrefab(GameObject go, string dir)
    {
        PrefabUtility.SaveAsPrefabAsset(go, dir + "/" + go.name + ".prefab");
        Object.DestroyImmediate(go);
    }

    static void AddPointLight(GameObject parent, Color color, float range, float intensity, string name = "PointLight")
    {
        GameObject lightGo = new GameObject(name);
        lightGo.transform.SetParent(parent.transform);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.range = range;
        light.intensity = intensity;
    }

    static GameObject CreateParticleChild(GameObject parent, string name, Color color, float size, float speed, float lifetime, float rate, bool loop = false)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startColor = color;
        main.startSize = size;
        main.startSpeed = speed;
        main.startLifetime = lifetime;
        main.loop = loop;
        main.duration = lifetime;
        var emission = ps.emission;
        emission.rateOverTime = rate;
        return go;
    }

    static void CreateSkillFire(string dir)
    {
        GameObject go = CreateBaseEffect("FX_Skill_Fire", new Color(1f, 0.2f, 0.05f), 1.5f);
        var main = go.GetComponent<ParticleSystem>().main;
        main.startSpeed = 3f;
        main.maxParticles = 100;
        var emission = go.GetComponent<ParticleSystem>().emission;
        emission.rateOverTime = 50;
        var shape = go.GetComponent<ParticleSystem>().shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.material = new Material(Shader.Find("Particles/Additive"));
        AddPointLight(go, new Color(1f, 0.3f, 0f), 8f, 2f);
        SavePrefab(go, dir);
    }

    static void CreateSkillIce(string dir)
    {
        GameObject go = CreateBaseEffect("FX_Skill_Ice", new Color(0.2f, 0.6f, 1f), 1.8f);
        var main = go.GetComponent<ParticleSystem>().main;
        main.startSpeed = 2f;
        main.maxParticles = 80;
        main.startRotation = 45f;
        var emission = go.GetComponent<ParticleSystem>().emission;
        emission.rateOverTime = 40;
        var shape = go.GetComponent<ParticleSystem>().shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 15f;
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        renderer.material = new Material(Shader.Find("Particles/Additive"));
        CreateParticleChild(go, "Shards", new Color(0.5f, 0.8f, 1f, 0.8f), 0.15f, 4f, 0.6f, 20f);
        SavePrefab(go, dir);
    }

    static void CreateSkillLightning(string dir)
    {
        GameObject go = CreateBaseEffect("FX_Skill_Lightning", Color.white, 0.8f);
        var main = go.GetComponent<ParticleSystem>().main;
        main.startSpeed = 8f;
        main.maxParticles = 60;
        var emission = go.GetComponent<ParticleSystem>().emission;
        emission.rateOverTime = 30;
        var shape = go.GetComponent<ParticleSystem>().shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(0.2f, 2f, 0.2f);
        var velocity = go.GetComponent<ParticleSystem>().velocityOverLifetime;
        velocity.enabled = true;
        velocity.y = new ParticleSystem.MinMaxCurve(-2f, 2f);
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.material = new Material(Shader.Find("Particles/Additive"));
        AddPointLight(go, new Color(0.8f, 0.9f, 1f), 10f, 3f, "BeamLight");
        SavePrefab(go, dir);
    }

    static void CreateSkillHeal(string dir)
    {
        GameObject go = CreateBaseEffect("FX_Skill_Heal", new Color(0.2f, 1f, 0.3f), 2f);
        var main = go.GetComponent<ParticleSystem>().main;
        main.startSpeed = -1.5f;
        main.maxParticles = 50;
        var emission = go.GetComponent<ParticleSystem>().emission;
        emission.rateOverTime = 25;
        var shape = go.GetComponent<ParticleSystem>().shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        renderer.material = new Material(Shader.Find("Particles/Additive"));
        AddPointLight(go, new Color(0.2f, 1f, 0.3f), 6f, 1.5f, "HealLight");
        SavePrefab(go, dir);
    }

    static void CreateSkillWind(string dir)
    {
        GameObject go = CreateBaseEffect("FX_Skill_Wind", new Color(0f, 0.8f, 0.8f), 1.2f);
        var main = go.GetComponent<ParticleSystem>().main;
        main.startSpeed = 5f;
        main.maxParticles = 120;
        var emission = go.GetComponent<ParticleSystem>().emission;
        emission.rateOverTime = 60;
        var shape = go.GetComponent<ParticleSystem>().shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.8f;
        var rotation = go.GetComponent<ParticleSystem>().rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-180f, 180f);
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.material = new Material(Shader.Find("Particles/Additive"));
        SavePrefab(go, dir);
    }

    static void CreateBuffAttack(string dir)
    {
        GameObject go = CreateBaseEffect("FX_Buff_Attack", new Color(1f, 0.8f, 0f), 3f);
        var main = go.GetComponent<ParticleSystem>().main;
        main.startSpeed = 1f;
        main.maxParticles = 30;
        main.loop = true;
        var emission = go.GetComponent<ParticleSystem>().emission;
        emission.rateOverTime = 10;
        var shape = go.GetComponent<ParticleSystem>().shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.6f;
        var rotation = go.GetComponent<ParticleSystem>().rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(0f, 360f);
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        renderer.material = new Material(Shader.Find("Particles/Additive"));
        AddPointLight(go, new Color(1f, 0.8f, 0f), 4f, 1f, "BuffLight");
        SavePrefab(go, dir);
    }

    static void CreateBuffDefense(string dir)
    {
        GameObject go = CreateBaseEffect("FX_Buff_Defense", new Color(0.2f, 0.5f, 1f), 3f);
        var main = go.GetComponent<ParticleSystem>().main;
        main.startSpeed = 0.5f;
        main.maxParticles = 40;
        main.loop = true;
        var emission = go.GetComponent<ParticleSystem>().emission;
        emission.rateOverTime = 15;
        var shape = go.GetComponent<ParticleSystem>().shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.7f;
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        renderer.material = new Material(Shader.Find("Particles/Additive"));
        CreateParticleChild(go, "ShieldGlow", new Color(0.3f, 0.6f, 1f, 0.4f), 1.2f, 0f, 3f, 8f);
        SavePrefab(go, dir);
    }

    static void CreateHitSmall(string dir)
    {
        GameObject go = CreateBaseEffect("FX_Hit_Small", Color.white, 0.3f);
        var main = go.GetComponent<ParticleSystem>().main;
        main.startSpeed = 2f;
        main.maxParticles = 10;
        main.startSize = 0.15f;
        var emission = go.GetComponent<ParticleSystem>().emission;
        emission.rateOverTime = 20;
        var shape = go.GetComponent<ParticleSystem>().shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = new Material(Shader.Find("Particles/Additive"));
        SavePrefab(go, dir);
    }

    static void CreateHitBig(string dir)
    {
        GameObject go = CreateBaseEffect("FX_Hit_Big", new Color(1f, 0.1f, 0.05f), 0.5f);
        var main = go.GetComponent<ParticleSystem>().main;
        main.startSpeed = 4f;
        main.maxParticles = 30;
        main.startSize = 0.4f;
        var emission = go.GetComponent<ParticleSystem>().emission;
        emission.rateOverTime = 40;
        var shape = go.GetComponent<ParticleSystem>().shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = new Material(Shader.Find("Particles/Additive"));
        var burst = new ParticleSystem.Burst(0f, 15);
        emission.SetBurst(0, burst);
        AddPointLight(go, new Color(1f, 0.2f, 0f), 6f, 2.5f, "HitLight");
        SavePrefab(go, dir);
    }
}
