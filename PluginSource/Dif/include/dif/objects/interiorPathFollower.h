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

#ifndef dif_interiorPathFollower_h
#define dif_interiorPathFollower_h

#include <dif/base/types.h>

DIF_NAMESPACE

class InteriorPathFollower : public Readable, public Writable {
public:
	class WayPoint : public Readable, public Writable {
	public:
		glm::vec3 position;
		glm::quat rotation;
		U32 msToNext;
		U32 smoothingType;

		WayPoint() : msToNext(0), smoothingType(0) {};

		virtual bool read(std::istream &stream, Version &version);
		virtual bool write(std::ostream &stream, Version version) const;
	};

	std::string name;
	std::string datablock;
	U32 interiorResIndex;
	glm::vec3 offset;
	Dictionary properties;

	std::vector<U32> triggerId;
	std::vector<WayPoint> wayPoint;

	U32 totalMS;

	InteriorPathFollower() : name(""), datablock(""), interiorResIndex(0), totalMS(0) {};

	/**
	 * Reads an InteriorPathFollower from a stream
	 * @param stream The stream to read from
	 * @return If the operation was successful
	 */
	virtual bool read(std::istream &stream, Version &version);
	/**
	 * Writes a InteriorPathFollower to a stream
	 * @param stream The stream to write to
	 * @return If the operation was successful
	 */
	virtual bool write(std::ostream &stream, Version version) const;
};

DIF_NAMESPACE_END

#endif
