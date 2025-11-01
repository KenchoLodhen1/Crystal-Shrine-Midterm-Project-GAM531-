#version 330 core
out vec4 FragColor;

in vec3 FragPos;
in vec3 Normal;
in vec2 TexCoord;

uniform vec3 viewPos;
uniform vec3 lightPos;
uniform vec3 lightColor;

uniform vec3 baseColor;
uniform float transparency;
uniform float shininess;
uniform float glowStrength;
uniform vec3 emissiveColor;

uniform bool useTexture;
uniform sampler2D texture0;

void main()
{
    vec3 norm = normalize(Normal);
    vec3 lightDir = normalize(lightPos - FragPos);
    vec3 viewDir = normalize(viewPos - FragPos);
    vec3 halfwayDir = normalize(lightDir + viewDir);

    // Basic Phong lighting
    float diff = max(dot(norm, lightDir), 0.0);
    float spec = pow(max(dot(norm, halfwayDir), 0.0), shininess);

    vec3 ambient  = baseColor * 0.1;
    vec3 diffuse  = baseColor * diff * lightColor;
    vec3 specular = vec3(1.0) * spec;

    // Fresnel edge glow
    float fresnel = pow(1.0 - max(dot(viewDir, norm), 0.0), 3.0);
    vec3 fresnelGlow = fresnel * vec3(0.6, 0.9, 1.0) * glowStrength;

    // Combine base light with glow
vec3 color = ambient + diffuse + specular + fresnelGlow;
color = mix(color, baseColor, 0.4) + baseColor * glowStrength * 0.5;

// Add emissive glow
color += emissiveColor * 0.5;

// Texture (symbol detail)
float texAlpha = 1.0;
if (useTexture)
{
    vec4 tex = texture(texture0, TexCoord);
    
    // Multiply lighting with texture
    color *= tex.rgb;
    
    // Add extra glow to textured parts (runes)
    color += emissiveColor * tex.rgb * 0.8;
    
    // Properly discard transparent pixels from texture
    if (tex.a < 0.1)
        discard;
    
    // Respect alpha from texture
    texAlpha = tex.a;
}

// Final alpha blend
float alpha = texAlpha * (1.0 - transparency);

FragColor = vec4(color, alpha);
}