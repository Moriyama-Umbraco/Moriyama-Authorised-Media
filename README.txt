Moriyama Authorised Media

Back office login
-----------------

darren@moriyama.co.uk
abc123abc123

Overview
--------

For Umbraco 7.7+ (not v8) - Will protected media in folders based on membergroups.

Create a media type with an alias "protectedFolder" and a property memberGroups which is comma delimited list of membergroups. A member must be in one of these groups to access items in the folder.

Needs the following supporting database table:

create table AuthorisedMedia(
MediaPathId int not null,
MediaParentId int not null
)

CREATE UNIQUE INDEX AuthorisedMediaIdx
ON AuthorisedMedia(MediaPathId, MediaParentId);

Also - add the following HTTP module - before imageprocessor:

<add name="AuthorisedMediaHttpModule" type="Moriyama.AuthorisedMedia.Application.AuthorisedMediaHttpModule, Moriyama.AuthorisedMedia" />

How it works
------------

On media save, an entry is recorded in the database for the media item if it has a parent media type "protectedFolder" - this is also removed on save, should the item be moved out of a protected folder.

An http module checks inbound requests for paths mathing /media/{folderId}/filename

If matched, it will check the database table to see if there is protection on the item. for performance non protected items are bypassed for a minute - which avoids too many database queries.

If an item is protected the request is redirected to a surface controller which will check member group permissions against the current member.

Should the permissions not match - then a 404 status is returned.

Permission checks are bypassed should you be logged in as a back office user.

Restrictions
------------

Recursion of protected folder properties does not occur down the tree. 
