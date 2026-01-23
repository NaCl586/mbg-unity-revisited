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

#ifndef dif_io_h
#define dif_io_h

#include <dif/base/types.h>
#include <type_traits>
#include <map>
#include <iostream>
#include <functional>
#include <assert.h>

DIF_NAMESPACE

#define LIGHT_MAP_SIZE 0x400

//#ifdef _WIN32
//#define DIR_SEP '\\'
//#else
#define DIR_SEP '/'
//#endif

//Define this to activate stream debug read/write logging
#if 0
#define PRINT_DEBUG_INFO
#endif

class IO {
public:
#ifdef PRINT_DEBUG_INFO
	//Print out information about the data being read / written to streams.
	// READCHECK and WRITECHECK will automatically append the type to name, normal
	// raw IO::read/write methods won't.
	template <typename T>
	static inline void debug_print(std::istream &stream, T &value, const std::string &name) {
		std::istream::pos_type pos = stream.tellg();
		//Basic information about the data's name / offset
		printf("Read %s at offset %lu (0x%lX)\n", name.c_str(), size_t(pos), size_t(pos));
	}
	template <typename T>
	static inline void debug_print(std::ostream &stream, const T &value, const std::string &name) {
		std::istream::pos_type pos = stream.tellp();
		//Basic information about the data's name / offset
		printf("Write %s at offset %lu (0x%lX)\n", name.c_str(), size_t(pos), size_t(pos));
	}
	template <typename T>
	static inline void debug_print_value(std::istream &stream, T &value, const std::string &name) {
		std::istream::pos_type pos = stream.tellg();
		//Basic information about the data's name / offset
		printf("Read %s at offset %lu (0x%lX): 0x%X / %u\n", name.c_str(), size_t(pos) - sizeof(T), size_t(pos) - sizeof(T), value, value);
	}
	template <typename T>
	static inline void debug_print_value(std::ostream &stream, const T &value, const std::string &name) {
		std::istream::pos_type pos = stream.tellp();
		//Basic information about the data's name / offset
		printf("Write %s at offset %lu (0x%lX): 0x%X / %u\n", name.c_str(), size_t(pos), size_t(pos), value, value);
	}
	static inline void debug_print_value(std::istream &stream, F32 &value, const std::string &name) {
		std::istream::pos_type pos = stream.tellg();
		//Basic information about the data's name / offset
		printf("Read %s at offset %lu (0x%lX): 0x%08X / %f\n", name.c_str(), size_t(pos) - sizeof(F32), size_t(pos) - sizeof(F32), *reinterpret_cast<U32 *>(static_cast<F32 *>(&value)), value);
	}
	static inline void debug_print_value(std::ostream &stream, const F32 &value, const std::string &name) {
		std::istream::pos_type pos = stream.tellp();
		//Basic information about the data's name / offset
		printf("Write %s at offset %lu (0x%lX): 0x%08X / %f\n", name.c_str(), size_t(pos), size_t(pos), *reinterpret_cast<const U32 *>(static_cast<const F32 *>(&value)), value);
	}

	//Print out information about the data being read / written to streams.
	// READCHECK and WRITECHECK will automatically append the type to name, normal
	// raw IO::read/write methods won't.
	template <typename T>
	static inline void debug_error(std::istream &stream, T &value, const std::string &name) {
		std::istream::pos_type pos = stream.tellg();
		//Basic information about the data's name / offset
		std::cout << "[Error] Read " << name << " at offset " << pos << " (0x" << std::hex << pos << ")" << std::dec << std::endl;
	}
	template <typename T>
	static inline void debug_error(std::ostream &stream, const T &value, const std::string &name) {
		std::istream::pos_type pos = stream.tellp();
		//Basic information about the data's name / offset
		std::cout << "[Error] Write " << name << " at offset " << pos << " (0x" << std::hex << pos << ")" << std::dec << std::endl;
	}
#else
	//Use a macro here so that absolutely no code is generated.
#define debug_print(stream, value, name)
#define debug_print_value(stream, value, name)
	//Use a macro here so that absolutely no code is generated.
#define debug_error(stream, value, name)
#endif

	//Read primitive types from a std::istream
	template <typename T, bool=true>
	struct read_impl {
		static inline bool read(std::istream &stream, Version &version, T &value, const std::string &name) {
			if (stream.eof()) {
				debug_error(stream, value, name);
				return false;
			}
			stream.read(reinterpret_cast<char *>(&value), sizeof(value));
			debug_print_value(stream, value, name);
			bool success = stream.good();
			if (!success) {
				debug_error(stream, value, name);
			}
			return success;
		}
	};
	//Read structures from a std::istream
	template <typename T>
	struct read_impl<T, false> {
		static inline bool read(std::istream &stream, Version &version, T &value, const std::string &name) {
			debug_print(stream, value, name);
			value.fileOffset = stream.tellg();
			bool success = (value.read(stream, version) && stream.good());
			if (!success) {
				debug_error(stream, value, name);
			} else {
				value.fileSize = int(stream.tellg()) - value.fileOffset;
			}
			return success;
		}
	};

	/**
	 * Read types from a stream
	 * @param stream The stream from which the data is read
	 * @param value A reference into which the data will be read
	 * @param name A string containing the name of the variable (for debugging)
	 * @return If the operation was successful
	 */
	template <typename T>
	static inline bool read(std::istream &stream, Version &version, T &value, const std::string &name) {
		//This will select one of the two read_impls above based on whether or not
		// T is a struct or a primitive type.
		return read_impl<T, std::is_fundamental<T>::value>::read(stream, version, value, name);
	}

	/**
	 * Read a vector from a stream
	 * @param stream The stream from which the data is read
	 * @param value A reference into which the data will be read
	 * @param name A string containing the name of the variable (for debugging)
	 * @return If the operation was successful
	 */
	template <typename T>
	static inline bool read(std::istream &stream, Version &version, std::vector<T> &value, const std::string &name) {
		//Read the size of the vector
		U32 size;
		if (!read(stream, version, size, name + " size"))
			return false;
		//Reserve some space
		value.reserve(size);

		//Read all the objects
		for (U32 i = 0; i < size; i ++) {
			T obj;
			//Make sure the read succeeds
			if (read(stream, version, obj, name + " obj"))
				value.push_back(obj);
			else
				return false;
		}

		return true;
	}

	/**
	 * Read a string from a stream
	 * @param stream The stream from which the data is read
	 * @param value A reference into which the data will be read
	 * @param name A string containing the name of the variable (for debugging)
	 * @return If the operation was successful
	 */
	static inline bool read(std::istream &stream, Version &version, std::string &value, const std::string &name) {
		//How long is the string
		U8 length;
		if (!read(stream, version, length, name + " length"))
			return false;
		//Empty the string
		value = std::string();
		//Read each byte of the string
		for (U32 i = 0; i < length; i ++) {
			//If we can read the byte, append it to the string
			U8 chr;
			if (read(stream, version, chr, name + " chr"))
				value += chr;
			else
				return false;
		}

		return true;
	}

	/**
	 * Read a dictionary from a stream
	 * @param stream The stream from which the data is read
	 * @param value A reference into which the data will be read
	 * @param name A string containing the name of the variable (for debugging)
	 * @return If the operation was successful
	 */
	static inline bool read(std::istream &stream, Version &version, Dictionary &value, const std::string &name) {
		//How long is the map
		U32 length;
		if (!read(stream, version, length, name + " length"))
			return false;

		//Empty the map
		value = Dictionary();

		//Read the map strings
		for (U32 i = 0; i < length; i ++) {
			std::string name, val;

			//Make sure we can read it
			if (!read(stream, version, name, name + " name") ||
			    !read(stream, version, val, name + " val")) {
				return false;
			}

			//Insert the pair
			value.push_back(Dictionary::value_type(name, val));
		}

		return true;
	}

	/**
	 * Read a vector from a stream, with the option to read as a secondary type if
	 * a given condition passes.
	 * @param stream The stream from which the data is read
	 * @param value A reference into which the data will be read
	 * @param condition A function that determines whether the second type should be used.
	 *                    Arguments are (bool isSigned, U32 param).
	 * @param name A string containing the name of the variable (for debugging)
	 * @return If the operation was successful
	 */
	template <typename type1, typename type2>
	static inline bool read_as(std::istream &stream, Version &version, std::vector<type1> &value, std::function<bool(bool,U32)> condition, const std::string &name) {
		//Read the size of the vector
		U32 size;
		if (!read(stream, version, size, name + " size"))
			return false;

		//Lots of index lists here that have U16 or U32 versions based on the sign bit.
		// The actual bytes of the interior have 0x80s at the ends (negative bit)
		// which seems to specify that these take a smaller type. They managed to
		// save ~50KB/interior, but was it worth the pain?

		//Params to use for the condition
		bool isSigned = false;
		U8 param = 0;

		//Should we use the alternate version?
		if (size & 0x80000000) {
			//Flip the sign bit
			size ^= 0x80000000;
			isSigned = true;

			//Extra U8 of data in each of these, almost never used but still there
			if (!read(stream, version, param, name + " param"))
				return false;
		}

		//Reserve some space
		value.reserve(size);

		//Read all the objects
		for (U32 i = 0; i < size; i ++) {
			//Should we use the alternate type? Lambda functions to the rescue!
			if (condition(isSigned, param)) {
				type2 obj;
				//Make sure the read succeeds
				if (read(stream, version, obj, name + " obj"))
					//Cast it back to what we want
					value.push_back(static_cast<type1>(obj));
				else
					return false;
			} else {
				type1 obj;
				//Make sure the read succeeds
				if (read(stream, version, obj, name + " obj"))
					value.push_back(obj);
				else
					return false;
			}
		}

		return true;
	}

	/**
	 * Read a vector from a stream, but use a given method for reading instead of 
	 * the standard call to read().
	 * @param stream The stream from which the data is read
	 * @param value A reference into which the data will be read
	 * @param passed_method A function which will read the object's fields from the stream.
	 *                        Arguments are (T &object, std::istream &stream)
	 * @param name A string containing the name of the variable (for debugging)
	 * @return If the operation was successful
	 */
	template <typename T>
	static inline bool read_with(std::istream &stream, Version &version, std::vector<T> &value, std::function<bool(T&, std::istream &, Version &)> passed_method, const std::string &name) {
		//Read the size of the vector
		U32 size;
		if (!read(stream, version, size, name + " size"))
			return false;
		//Reserve some space
		value.reserve(size);

		//Read all the objects
		for (U32 i = 0; i < size; i ++) {
			T obj;
			//Make sure the read succeeds
			if (passed_method(obj, stream, version))
				value.push_back(obj);
			else
				return false;
		}

		return true;
	}

	/**
	 * Read a vector from a stream, but call an extra method after the size is read,
	 * before the actual contents are read.
	 * @param stream The stream from which the data is read
	 * @param value A reference into which the data will be read
	 * @param extra_method A function that will be called before the contents are read.
	 *                       Arguments are (std::istream &stream)
	 * @param name A string containing the name of the variable (for debugging)
	 * @return If the operation was successful
	 */
	template <typename T>
	static inline bool read_extra(std::istream &stream, Version &version, std::vector<T> &value, std::function<bool(std::istream &, Version &)> extra_method, const std::string &name) {
		//Read the size of the vector
		U32 size;
		if (!read(stream, version, size, name + " size"))
			return false;
		//Reserve some space
		value.reserve(size);

		//Do the extra method
		if (!extra_method(stream, version))
			return false;

		//Read all the objects
		for (U32 i = 0; i < size; i ++) {
			T obj;
			//Make sure the read succeeds
			if (read(stream, version, obj, name + " obj"))
				value.push_back(obj);
			else
				return false;
		}

		return true;
	}


	//Write primitive types to a std::ostream
	template <typename T, bool=true>
	struct write_impl {
		static inline bool write(std::ostream &stream, Version version, const T &value, const std::string &name) {
			debug_print_value(stream, value, name);
			stream.write(reinterpret_cast<const char *>(&value), sizeof(value));
			return stream.good();
		}
	};
	//Write structures to a std::ostream
	template <typename T>
	struct write_impl<T, false> {
		static inline bool write(std::ostream &stream, Version version, const T &value, const std::string &name) {
			debug_print(stream, value, name);
			return value.write(stream, version);
		}
	};

	/**
	 * Write types to a stream
	 * @param stream The stream to which the data is written
	 * @param value The value of the data to write
	 * @param name A string containing the name of the variable (for debugging)
	 * @return If the operation was successful
	 */
	template <typename T>
	static inline bool write(std::ostream &stream, Version version, const T &value, const std::string &name) {
		//This will select one of the two write_impls above based on whether or not
		// T is a struct or a primitive type.
		return write_impl<T, std::is_fundamental<T>::value>::write(stream, version, value, name);
	}

	/**
	 * Write a vector to a stream
	 * @param stream The stream to which the data is written
	 * @param value The vector to write
	 * @param name A string containing the name of the variable (for debugging)
	 * @return If the operation was successful
	 */
	template <typename T>
	static inline bool write(std::ostream &stream, Version version, const std::vector<T> &value, const std::string &name) {
		//Write the vector's size first, must be a U32 because torque
		if (!write(stream, version, static_cast<U32>(value.size()), name + " size"))
			return false;
		//Write all of the objects in the vector
		for (size_t i = 0; i < value.size(); i ++) {
			if (!write(stream, version, value[i], name + " value"))
				return false;
		}
		return true;
	}

	/**
	 * Write a string to a stream
	 * @param stream The stream to which the data is written
	 * @param value The string to write
	 * @param name A string containing the name of the variable (for debugging)
	 * @return If the operation was successful
	 */
	static inline bool write(std::ostream &stream, Version version, const std::string &value, const std::string &name) {
		//How long is the string
		if (!write(stream, version, static_cast<U8>(value.length()), name + " length"))
			return false;
		//Write each byte of the string
		for (U32 i = 0; i < value.length(); i ++) {
			if (!write(stream, version, value[i], name + " char"))
				return false;
		}

		return true;
	}

	/**
	 * Write a dictionary to a stream
	 * @param stream The stream to which the data is written
	 * @param value The map to write
	 * @param name A string containing the name of the variable (for debugging)
	 * @return If the operation was successful
	 */
	static inline bool write(std::ostream &stream, Version version, const Dictionary &value, const std::string &name) {
		//How long is the map
		if (!write(stream, version, static_cast<U32>(value.size()), name + " length"))
			return false;
		
		//Write each string in the map
		for (auto pair : value) {
			if (!write(stream, version, pair.first, name + " name") ||
			    !write(stream, version, pair.second, name + " value"))
				return false;
		}

		return true;
	}

	/**
	 * Write a vector to a stream, but call an extra method after the size is written,
	 * before the actual contents are written.
	 * @param stream The stream to which the data is written
	 * @param value The vector to write
	 * @param extra_method A function that will be called before the contents are written.
	 *                       Arguments are (std::ostream &stream)
	 * @param name A string containing the name of the variable (for debugging)
	 * @return If the operation was successful
	 */
	template <typename T>
	static inline bool write_extra(std::ostream &stream, Version version, const std::vector<T> &value, std::function<bool(std::ostream &, Version)> extra_method, const std::string &name) {
		//Write the vector's size first, must be a U32 because torque
		if (!write(stream, version, static_cast<U32>(value.size()), name + " size"))
			return false;
		//Use the extra method... which happens to come before the value
		if (!extra_method(stream, version))
			return false;

		//Write all of the objects in the vector
		for (size_t i = 0; i < value.size(); i ++) {
			if (!write(stream, version, value[i], name + " value"))
				return false;
		}
		return true;
	}
};

template <>
inline bool IO::read<glm::ivec2>(std::istream &stream, Version &version, glm::ivec2 &value, const std::string &name) {
	return
		IO::read(stream, version, value.x, name + " x") &&
		IO::read(stream, version, value.y, name + " y");
}

template <>
inline bool IO::read<glm::vec2>(std::istream &stream, Version &version, glm::vec2 &value, const std::string &name) {
	return
		IO::read(stream, version, value.x, name + " x") &&
		IO::read(stream, version, value.y, name + " y");
}

template <>
inline bool IO::read<glm::vec3>(std::istream &stream, Version &version, glm::vec3 &value, const std::string &name) {
	return
		IO::read(stream, version, value.x, name + " x") &&
		IO::read(stream, version, value.y, name + " y") &&
		IO::read(stream, version, value.z, name + " z");
}

template <>
inline bool IO::read<glm::cvec4>(std::istream &stream, Version &version, glm::cvec4 &value, const std::string &name) {
	return
		IO::read(stream, version, value.r, name + " red") &&
		IO::read(stream, version, value.g, name + " green") &&
		IO::read(stream, version, value.b, name + " blue") &&
		IO::read(stream, version, value.a, name + " alpha");
}

template <>
inline bool IO::read<glm::quat>(std::istream &stream, Version &version, glm::quat &value, const std::string &name) {
	return
		IO::read(stream, version, value.w, name + "w") &&
		IO::read(stream, version, value.x, name + "x") &&
		IO::read(stream, version, value.y, name + "y") &&
		IO::read(stream, version, value.z, name + "z");
}

template<>
inline bool IO::read<glm::mat4>(std::istream &stream, Version &version, glm::mat4 &value, const std::string &name) {
	return
		IO::read(stream, version, value[0][0], name + "[0][0]") &&
		IO::read(stream, version, value[1][0], name + "[1][0]") &&
		IO::read(stream, version, value[2][0], name + "[2][0]") &&
		IO::read(stream, version, value[3][0], name + "[3][0]") &&
		IO::read(stream, version, value[0][1], name + "[0][1]") &&
		IO::read(stream, version, value[1][1], name + "[1][1]") &&
		IO::read(stream, version, value[2][1], name + "[2][1]") &&
		IO::read(stream, version, value[3][1], name + "[3][1]") &&
		IO::read(stream, version, value[0][2], name + "[0][2]") &&
		IO::read(stream, version, value[1][2], name + "[1][2]") &&
		IO::read(stream, version, value[2][2], name + "[2][2]") &&
		IO::read(stream, version, value[3][2], name + "[3][2]") &&
		IO::read(stream, version, value[0][3], name + "[0][3]") &&
		IO::read(stream, version, value[1][3], name + "[1][3]") &&
		IO::read(stream, version, value[2][3], name + "[2][3]") &&
		IO::read(stream, version, value[3][3], name + "[3][3]");
}

template <>
inline bool IO::write<glm::ivec2>(std::ostream &stream, Version version, const glm::ivec2 &value, const std::string &name) {
	return
		IO::write(stream, version, value.x, name + " x") &&
		IO::write(stream, version, value.y, name + " y");
}

template <>
inline bool IO::write<glm::vec2>(std::ostream &stream, Version version, const glm::vec2 &value, const std::string &name) {
	return
		IO::write(stream, version, value.x, name + " x") &&
		IO::write(stream, version, value.y, name + " y");
}

template <>
inline bool IO::write<glm::vec3>(std::ostream &stream, Version version, const glm::vec3 &value, const std::string &name) {
	return
		IO::write(stream, version, value.x, name + " x") &&
		IO::write(stream, version, value.y, name + " y") &&
		IO::write(stream, version, value.z, name + " z");
}

template <>
inline bool IO::write<glm::cvec4>(std::ostream &stream, Version version, const glm::cvec4 &value, const std::string &name) {
	return
		IO::write(stream, version, value.r, name + " red") &&
		IO::write(stream, version, value.g, name + " green") &&
		IO::write(stream, version, value.b, name + " blue") &&
		IO::write(stream, version, value.a, name + " alpha");
}

template <>
inline bool IO::write<glm::quat>(std::ostream &stream, Version version, const glm::quat &value, const std::string &name) {
	return
		IO::write(stream, version, value.w, "w") &&
		IO::write(stream, version, value.x, "x") &&
		IO::write(stream, version, value.y, "y") &&
		IO::write(stream, version, value.z, "z");
}

template<>
inline bool IO::write<glm::mat4>(std::ostream &stream, Version version, const glm::mat4 &value, const std::string &name) {
	return
		IO::write(stream, version, value[0][0], name + "[0][0]") &&
		IO::write(stream, version, value[1][0], name + "[1][0]") &&
		IO::write(stream, version, value[2][0], name + "[2][0]") &&
		IO::write(stream, version, value[3][0], name + "[3][0]") &&
		IO::write(stream, version, value[0][1], name + "[0][1]") &&
		IO::write(stream, version, value[1][1], name + "[1][1]") &&
		IO::write(stream, version, value[2][1], name + "[2][1]") &&
		IO::write(stream, version, value[3][1], name + "[3][1]") &&
		IO::write(stream, version, value[0][2], name + "[0][2]") &&
		IO::write(stream, version, value[1][2], name + "[1][2]") &&
		IO::write(stream, version, value[2][2], name + "[2][2]") &&
		IO::write(stream, version, value[3][2], name + "[3][2]") &&
		IO::write(stream, version, value[0][3], name + "[0][3]") &&
		IO::write(stream, version, value[1][3], name + "[1][3]") &&
		IO::write(stream, version, value[2][3], name + "[2][3]") &&
		IO::write(stream, version, value[3][3], name + "[3][3]");
}

//Slightly hacky read function so we don't get weird memory errors when trying to
// read a U32 into a U8. Also no reinterpret_cast because those are screwy
template <typename T, typename F>
inline bool __cast_read(std::istream &stream, Version &version, F &thing, const std::string &name) {
	//Temp of the type we want
	T temp{};
	//Make sure the read actually works
	if (!IO::read(stream, version, temp, name))
		return false;
	//And then just copy into our variable
	thing = F(temp);
	return true;
}

//Write function in the same idea as the read function, just backwards
template <typename T, typename F>
inline bool __cast_write(std::ostream &stream, Version &version, const F &thing, const std::string &name) {
	//Literally just cast and write
	T temp(thing);
	return IO::write(stream, version, temp, name);
}

//Macros to speed up file reading/writing
#ifdef PRINT_DEBUG_INFO
	#define READCHECK(name, type)  { if (!__cast_read<type, decltype(name)> (stream, version, name, #name " as " #type)) return false; }
	#define WRITECHECK(name, type) { if (!__cast_write<type, decltype(name)>(stream, version, name, #name " as " #type)) return false; }
#else
	#define READCHECK(name, type)  { if (!__cast_read<type, decltype(name)> (stream, version, name, "")) return false; }
	#define WRITECHECK(name, type) { if (!__cast_write<type, decltype(name)>(stream, version, name, "")) return false; }
#endif

DIF_NAMESPACE_END

#endif
