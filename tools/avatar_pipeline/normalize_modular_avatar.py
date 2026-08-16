import bpy
import json
import os
import sys

args = sys.argv[sys.argv.index('--') + 1:]
output_fbx, output_report = args[0], args[1]
armatures = [obj for obj in bpy.data.objects if obj.type == 'ARMATURE']
if len(armatures) != 1:
    raise RuntimeError('Expected exactly one armature, found %d' % len(armatures))
armature = armatures[0]
meshes = [obj for obj in bpy.data.objects if obj.type == 'MESH' and len(obj.vertex_groups) > 0]
repaired = []
for obj in meshes:
    # Old source variants share datablocks. Unique copies keep every swappable
    # object as a separate SkinnedMeshRenderer after FBX import in Unity.
    obj.data = obj.data.copy()
    modifiers = [modifier for modifier in obj.modifiers if modifier.type == 'ARMATURE']
    if not modifiers:
        modifier = obj.modifiers.new(name='CanonicalArmature', type='ARMATURE')
        modifier.object = armature
        repaired.append(obj.name)
    else:
        for modifier in modifiers:
            if modifier.object is None:
                modifier.object = armature
                repaired.append(obj.name)
    obj.hide = False
    obj.hide_render = False

bpy.ops.object.select_all(action='DESELECT')
armature.select = True
for obj in meshes:
    obj.select = True
bpy.context.scene.objects.active = armature
os.makedirs(os.path.dirname(output_fbx), exist_ok=True)
bpy.ops.export_scene.fbx(filepath=output_fbx, use_selection=True,
    object_types={'ARMATURE', 'MESH'}, use_mesh_modifiers=True,
    add_leaf_bones=False, bake_anim=False, axis_forward='-Z', axis_up='Y')

report = {
    'source': bpy.data.filepath,
    'output': output_fbx,
    'armature': armature.name,
    'bone_count': len(armature.data.bones),
    'mesh_count': len(meshes),
    'repaired_armature_modifiers': repaired,
    'total_vertices': sum(len(obj.data.vertices) for obj in meshes),
    'total_triangles': sum(sum(max(0, len(poly.vertices) - 2) for poly in obj.data.polygons) for obj in meshes),
}
with open(output_report, 'w') as handle:
    json.dump(report, handle, indent=2, sort_keys=True)
