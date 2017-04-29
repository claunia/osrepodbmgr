//
//  Author:
//    Natalia Portillo claunia@claunia.com
//
//  Copyright (c) 2017, © Claunia.com
//
//  All rights reserved.
//
//  Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
//
//     * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in
//       the documentation and/or other materials provided with the distribution.
//     * Neither the name of the [ORGANIZATION] nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
//
//  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
//  "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
//  LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
//  A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
//  CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
//  EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
//  PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
//  PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
//  LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
//  NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
//  SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//
namespace osrepodbmgr
{
    public static class Schema
    {
        public const string FilesTableSql = "-- -----------------------------------------------------\n" +
            "-- Table `files`\n" +
            "-- -----------------------------------------------------\n" +
            "DROP TABLE IF EXISTS `files` ;\n\n" +
            "CREATE TABLE IF NOT EXISTS `files` (\n" +
            "  `id` INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "  `sha256` VARCHAR(64) NOT NULL);\n\n" +
            "CREATE UNIQUE INDEX `files_id_UNIQUE` ON `files` (`id` ASC);\n\n" +
            "CREATE UNIQUE INDEX `files_sha256_UNIQUE` ON `files` (`sha256` ASC);";

        public const string OSesTableSql = "-- -----------------------------------------------------\n" +
            "-- Table `oses`\n" +
            "-- -----------------------------------------------------\n" +
            "DROP TABLE IF EXISTS `oses` ;\n\n" +
            "CREATE TABLE IF NOT EXISTS `oses` (\n" +
            "  `id` INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "  `developer` VARCHAR(45) NOT NULL,\n" +
            "  `product` VARCHAR(45) NOT NULL,\n" +
            "  `version` VARCHAR(45) NULL,\n" +
            "  `languages` VARCHAR(45) NULL,\n" +
            "  `architecture` VARCHAR(45) NULL,\n" +
            "  `machine` VARCHAR(45) NULL,\n" +
            "  `format` VARCHAR(45) NULL,\n" +
            "  `description` VARCHAR(45) NULL,\n" +
            "  `oem` BOOLEAN NULL,\n" +
            "  `upgrade` BOOLEAN NULL,\n" +
            "  `update` BOOLEAN NULL,\n" +
            "  `source` BOOLEAN NULL,\n" +
            "  `files` BOOLEAN NULL,\n" +
            "  `netinstall` BOOLEAN NULL,\n" +
            "  `xml` BLOB NULL,\n" +
            "  `json` BLOB NULL);\n\n" +
            "CREATE UNIQUE INDEX `oses_id_UNIQUE` ON `oses` (`id` ASC);\n\n" +
            "CREATE INDEX `oses_developer_idx` ON `oses` (`developer` ASC);\n\n" +
            "CREATE INDEX `oses_product_idx` ON `oses` (`product` ASC);\n\n" +
            "CREATE INDEX `oses_version_idx` ON `oses` (`version` ASC);\n\n" +
            "CREATE INDEX `oses_architecture_idx` ON `oses` (`architecture` ASC);\n\n" +
            "CREATE INDEX `oses_format_idx` ON `oses` (`format` ASC);\n\n" +
            "CREATE INDEX `oses_machine_idx` ON `oses` (`machine` ASC);\n\n" +
            "CREATE INDEX `oses_description_idx` ON `oses` (`description` ASC);";
    }
}

