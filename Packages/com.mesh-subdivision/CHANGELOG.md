# Changelog

## [0.2.0] 2020-08-09

### Added

### Change
 - Change management of buffers considering camera type
 - Move render passes into renderer feature
 - Update shaders followed by 19.3 urp
 - Scale by transform
 - Change packing/unpacking between subdivision buffer and subdivision var
 - Refactor culling in shader
 - Move render passes from component to render passes
 - Move serialization of shaders from component to renderer feature
 - Move loading meshes from in-editor to constructor and serialize them in component
 - Rename 'MeshSubdivisionRenderer' as 'MeshSubdivision'

### Fixed
 - Fix wrong calculating lod based by pixel coverage
 - Add dependency of package as urp

## [0.1.0] 2020-01-20

### Added
 - Add main lighting
 - Add base mesh(quad, cube, sphere)

### Changed
 - Change loading mesh from editor to setting by base in runtime
 - Change quad mesh size from 128 to one to match the scales among base meshes
 - Update Unity to 19.3

### Fixed
 - Fix initial state of mesh subdivision renderer after created
 - Add exception to parsing string to in/float

### Changelog Started