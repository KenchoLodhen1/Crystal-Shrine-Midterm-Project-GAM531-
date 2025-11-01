#version 330 core
out vec4 FragColor;

in vec3 FragPos;
in vec3 Normal;
in vec2 TexCoord;
in vec3 VertexColor;

uniform sampler2D texture0;
uniform vec3 viewPos;

uniform bool flashlightOn;
uniform vec3 flashlightPos;
uniform vec3 flashlightDir;
uniform vec3 emissiveColor;
uniform float flashlightIntensity;

uniform bool useTexture;
uniform vec3 objectColor;
uniform bool isFlying;
uniform bool useVertexColors;

// Dynamic point lights (pillars)
#define MAX_POINT_LIGHTS 5
uniform int numPointLights;
uniform vec3 pointLightPositions[MAX_POINT_LIGHTS];
uniform vec3 pointLightColors[MAX_POINT_LIGHTS];
uniform float pointLightIntensities[MAX_POINT_LIGHTS];

vec3 calculatePointLight(
    vec3 lightPos,
    vec3 lightColor,
    float lightIntensity,
    vec3 normal,
    vec3 fragPos,
    vec3 viewDir,
    vec3 baseColor)
{
    vec3 lightDir = normalize(lightPos - fragPos);
    float distance = length(lightPos - fragPos);

    // Attenuation
    float attenuation = lightIntensity / (1.0 + 0.09 * distance + 0.032 * distance * distance);

    // Diffuse
    float diff = max(dot(normal, lightDir), 0.0);
    vec3 diffuse = diff * lightColor * baseColor;

    // Specular
    vec3 reflectDir = reflect(-lightDir, normal);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), 32.0);
    vec3 specular = spec * lightColor * 0.5;

    return (diffuse + specular) * attenuation;
}

void main()
{
    vec3 baseColor;

    if (useTexture)
        baseColor = texture(texture0, TexCoord).rgb;
    else if (useVertexColors)
        baseColor = VertexColor;
    else
        baseColor = objectColor;

    vec3 norm = normalize(Normal);
    vec3 viewDir = normalize(viewPos - FragPos);

    // Ambient lighting
    float ambientStrength = isFlying ? 1.0 : 0.12;
    vec3 ambient = ambientStrength * baseColor;
    vec3 result = ambient;

    // Flashlight
    if (flashlightOn)
    {
        vec3 flashDir = normalize(flashlightPos - FragPos);
        float distance = length(flashlightPos - FragPos);

        // Attenuation
        float attenuation = 1.0 / (1.0 + 0.045 * distance + 0.0075 * distance * distance);

        // Spotlight cone
        vec3 spotDir = normalize(flashlightDir);
        float theta = dot(flashDir, -spotDir);
        float innerCone = cos(radians(15.0));
        float outerCone = cos(radians(30.0));
        float epsilon = innerCone - outerCone;
        float intensity = clamp((theta - outerCone) / epsilon, 0.0, 1.0);

        // Diffuse
        float diff = max(dot(norm, flashDir), 0.0);
        vec3 flashDiffuse = diff * vec3(1.0, 0.85, 0.6) * baseColor * flashlightIntensity;

        // Specular
        vec3 flashReflect = reflect(-flashDir, norm);
        float spec = pow(max(dot(viewDir, flashReflect), 0.0), 64.0);
        vec3 flashSpecular = spec * vec3(1.0, 0.85, 0.6) * 0.8;

        flashDiffuse *= attenuation * intensity;
        flashSpecular *= attenuation * intensity;

        result += flashDiffuse + flashSpecular;
    }

    // Dynamic point lights (pillars)
    for (int i = 0; i < numPointLights; i++)
    {
        result += calculatePointLight(
            pointLightPositions[i],
            pointLightColors[i],
            pointLightIntensities[i],
            norm,
            FragPos,
            viewDir,
            baseColor
        );
    }

    // Apply emissive (for glowing pillars, etc.)
    vec3 finalColor = result + emissiveColor;

    // Fog
    if (isFlying)
    {
        FragColor = vec4(finalColor, 1.0);
    }
    else
    {
        float fogDensity = 0.025;
        float d = length(viewPos - FragPos);
        float fogFactor = clamp(exp(-pow(d * fogDensity, 2.0)), 0.0, 1.0);
        vec3 fogColor = vec3(0.02, 0.02, 0.04);
        vec3 foggedSurface = mix(fogColor, finalColor, fogFactor);
        FragColor = vec4(foggedSurface, 1.0);
    }
}
