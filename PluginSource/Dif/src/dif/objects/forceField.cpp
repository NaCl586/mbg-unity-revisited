//------------------------------------------------------------------------------
// Copyright (c) 2015 HiGuy Smith
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//  * Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
//  * Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
//  * Neither the name of the project nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//------------------------------------------------------------------------------

#include <dif/base/io.h>
#include <dif/objects/forceField.h>

DIF_NAMESPACE

bool ForceField::read(std::istream &stream, Version &version) {
	READCHECK(forceFieldFileVersion, U32); //forceFieldFileVersion
	READCHECK(name, std::string); //name
	READCHECK(trigger, std::vector<std::string>); //trigger
	READCHECK(boundingBox, BoxF); //boundingBox
	READCHECK(boundingSphere, SphereF); //boundingSphere
	READCHECK(normal, std::vector<glm::vec3>); //normal
	READCHECK(plane, std::vector<Plane>); //plane
	READCHECK(bspNode, std::vector<BSPNode>); //BSPNode
	READCHECK(bspSolidLeaf, std::vector<BSPSolidLeaf>); //BSPSolidLeaf
	READCHECK(index, std::vector<U32>); //index
	READCHECK(surface, std::vector<Surface>); //surface
	READCHECK(solidLeafSurface, std::vector<U32>); //solidLeafSurface
	READCHECK(color, glm::cvec4); //color

	return true;
}

bool ForceField::write(std::ostream &stream, Version version) const {
	WRITECHECK(forceFieldFileVersion, U32); //forceFieldFileVersion
	WRITECHECK(name, std::string); //name
	WRITECHECK(trigger, std::vector<std::string>); //trigger
	WRITECHECK(boundingBox, BoxF); //boundingBox
	WRITECHECK(boundingSphere, SphereF); //boundingSphere
	WRITECHECK(normal, std::vector<glm::vec3>); //normal
	WRITECHECK(plane, std::vector<Plane>); //plane
	WRITECHECK(bspNode, std::vector<BSPNode>); //BSPNode
	WRITECHECK(bspSolidLeaf, std::vector<BSPSolidLeaf>); //BSPSolidLeaf
	WRITECHECK(index, std::vector<U32>); //index
	WRITECHECK(surface, std::vector<Surface>); //surface
	WRITECHECK(solidLeafSurface, std::vector<U32>); //solidLeafSurface
	WRITECHECK(color, glm::cvec4); //color

	return true;
}

bool ForceField::Plane::read(std::istream &stream, Version &version) {
	READCHECK(normalIndex, U32); //normalIndex
	READCHECK(planeDistance, F32); //planeDistance

	return true;
}

bool ForceField::Plane::write(std::ostream &stream, Version version) const {
	WRITECHECK(normalIndex, U32); //normalIndex
	WRITECHECK(planeDistance, F32); //planeDistance

	return true;
}

bool ForceField::BSPNode::read(std::istream &stream, Version &version) {
	READCHECK(frontIndex, U16); //frontIndex
	READCHECK(backIndex, U16); //backIndex

	return true;
}

bool ForceField::BSPNode::write(std::ostream &stream, Version version) const {
	WRITECHECK(frontIndex, U16); //frontIndex
	WRITECHECK(backIndex, U16); //backIndex

	return true;
}

bool ForceField::BSPSolidLeaf::read(std::istream &stream, Version &version) {
	READCHECK(surfaceIndex, U32); //surfaceIndex
	READCHECK(surfaceCount, U16); //surfaceCount

	return true;
}

bool ForceField::BSPSolidLeaf::write(std::ostream &stream, Version version) const {
	WRITECHECK(surfaceIndex, U32); //surfaceIndex
	WRITECHECK(surfaceCount, U16); //surfaceCount

	return true;
}

bool ForceField::Surface::read(std::istream &stream, Version &version) {
	READCHECK(windingStart, U32); //windingStart
	READCHECK(windingCount, U8); //windingCount
	READCHECK(planeIndex, U16); //planeIndex
	READCHECK(surfaceFlags, U8); //surfaceFlags
	READCHECK(fanMask, U32); //fanMask

	return true;
}

bool ForceField::Surface::write(std::ostream &stream, Version version) const {
	WRITECHECK(windingStart, U32); //windingStart
	WRITECHECK(windingCount, U8); //windingCount
	WRITECHECK(planeIndex, U16); //planeIndex
	WRITECHECK(surfaceFlags, U8); //surfaceFlags
	WRITECHECK(fanMask, U32); //fanMask

	return true;
}

DIF_NAMESPACE_END
