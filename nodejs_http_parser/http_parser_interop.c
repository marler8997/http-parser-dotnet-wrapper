#include "http_parser_interop.h"

size_t http_parser_sizeof_size_t()
{
	return sizeof(size_t);
}
void http_parser_get_sizes_and_offsets(struct http_parser_sizes_and_offsets* sizesAndOffsets)
{
	sizesAndOffsets->sizeofShort = sizeof(short);
	sizesAndOffsets->sizeofInt = sizeof(int);
	sizesAndOffsets->sizeofLong = sizeof(long);

	// http_parser offsets
	sizesAndOffsets->sizeofHttpParser                = sizeof(struct http_parser);
	sizesAndOffsets->offsetOfHttpParserNRead         = offsetof(struct http_parser, nread);
	sizesAndOffsets->offsetOfHttpParserContentLength = offsetof(struct http_parser, content_length);
	sizesAndOffsets->offsetOfHttpParserHttpMajor     = offsetof(struct http_parser, http_major);
	sizesAndOffsets->offsetOfHttpParserHttpMinor     = offsetof(struct http_parser, http_minor);
	sizesAndOffsets->offsetOfHttpParserData          = offsetof(struct http_parser, data);
	
	// http_parser_settings offsets
	sizesAndOffsets->sizeofHttpParserSettings                  = sizeof(struct http_parser_settings);
	sizesAndOffsets->offsetOfHttpParserSettingsOnMessageBegin  = offsetof(struct http_parser_settings, on_message_begin);
	sizesAndOffsets->offsetOfHttpParserSettingsOnUrl           = offsetof(struct http_parser_settings, on_url);
	sizesAndOffsets->offsetOfHttpParserSettingsOnChunkComplete = offsetof(struct http_parser_settings, on_chunk_complete);

}