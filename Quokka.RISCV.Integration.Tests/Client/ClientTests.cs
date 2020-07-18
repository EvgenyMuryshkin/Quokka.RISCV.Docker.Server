using Microsoft.VisualStudio.TestTools.UnitTesting;
using Quokka.RISCV.Integration.Client;
using Quokka.RISCV.Integration.DTO;
using Quokka.RISCV.Integration.Engine;
using Quokka.RISCV.Integration.Generator;
using Quokka.RISCV.Integration.Generator.SOC;
using Quokka.RISCV.Integration.Tests.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Quokka.RISCV.Docker.Server.Tests
{
    [TestClass]
    public class ClientTests
    {
        [TestMethod]
        public async Task Asm()
        {
            var asm = @"
addi x1, x0, 10
addi x1, x0, -10
";
            var instructions = await RISCVIntegrationClient.Asm(new RISCVIntegrationEndpoint(), asm);
            Assert.AreEqual(2, instructions.Length);
            Assert.AreEqual(0x00A00093U, instructions[0]);
            Assert.AreEqual(0xFF600093U, instructions[1]);
        }

        [TestMethod]
        public async Task ClientTest_Windows()
        {
            if (!Debugger.IsAttached)
                Assert.Inconclusive("Run local service and debug this test");

            var testDataRoot = Path.Combine(Directory.GetCurrentDirectory(), "client", "TestDataWindows");

            var context = new RISCVIntegrationContext()
                .WithEndpoint(new RISCVIntegrationEndpoint() { Port = 15001 })
                .WithExtensionClasses(new ExtensionClasses().Text("cmd"))
                .WithRootFolder(testDataRoot)
                .WithAllRegisteredFiles()
                .WithOperations(new CmdInvocation("1.cmd"))
                .TakeModifiedFiles()
                ;

            var result = await RISCVIntegrationClient.Run(context);

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task ClientTest_Docker()
        {
            if (!Debugger.IsAttached)
                Assert.Inconclusive("Run local service and debug this test");

            var testDataRoot = Path.Combine(Directory.GetCurrentDirectory(), "client", "TestDataDocker");

            var context = new RISCVIntegrationContext()
                .WithExtensionClasses(new ExtensionClasses().Text("sh"))
                .WithRootFolder(testDataRoot)
                .WithAllRegisteredFiles()
                .WithOperations(
                    new BashInvocation("chmod 777 ./1.sh"),
                    new ResetRules(),
//                    new BashInvocation("mkdir output"),
                    new BashInvocation("./1.sh")
                )
                .TakeModifiedFiles()
                ;

            var result = await RISCVIntegrationClient.Run(context);

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task ClientTest_Docker_TinyFPGA()
        {
            if (!Debugger.IsAttached)
                Assert.Inconclusive("Run local service and debug this test");

            var testDataRoot = Path.Combine(Directory.GetCurrentDirectory(), "client", "TinyFPGA-BX");

            var context = new RISCVIntegrationContext()
                .WithExtensionClasses(
                    new ExtensionClasses()
                        .Text("sh")
                        .Text("")
                        .Text("lds")
                        .Text("s")
                        .Text("c")
                        .Text("cpp")
                        .Binary("bin")
                        .Binary("elf")
                        .Text("map")
                )
                .WithRootFolder(testDataRoot)
                .WithAllRegisteredFiles()
                .WithOperations(
                    new BashInvocation("make firmware.bin")
                )
                .TakeModifiedFiles()
                ;

            var result = await RISCVIntegrationClient.Run(context);

            Assert.IsNotNull(result);

            var binFile = result.ResultSnapshot.Files.Find(f => f.Name == "firmware.bin");
            Assert.IsNotNull(binFile);
        }

        string TemplatesPath(string path)
        {
            var templateRoot = Path.Combine(path, "client", "Blinker", "Template");
            if (Directory.Exists(templateRoot))
                return templateRoot;

            return TemplatesPath(Path.GetDirectoryName(path));
        }

        [TestMethod]
        public async Task RISCV_Memory_UInt()
        {
            var externalData = new List<SOCRecord>()
            {
                new SOCRecord() {
                    Segment = 0x00,
                    DataType = typeof(uint),
                    Depth = 512,
                    SoftwareName = "l_mem",
                    HardwareName = "l_mem",
                    Template = "memory32"
                },
                new SOCRecord() {
                    Segment = 0x01,
                    DataType = typeof(uint),
                    Depth = 16,
                    SoftwareName = "data",
                    HardwareName = "data",
                    Template = "memory32"
                },
            };

            var mainCode = @"
    uint32_t counter = 0;
       
    while (counter < data_size) {
		data[counter] = counter;
        counter = data[counter] + 1;
    } 
";
            await RunWithData(externalData, mainCode);
        }

        [TestMethod]
        public async Task RISCV_Memory_UShort()
        {
            var externalData = new List<SOCRecord>()
            {
                new SOCRecord() {
                    Segment = 0x00,
                    DataType = typeof(uint),
                    Depth = 512,
                    SoftwareName = "l_mem",
                    HardwareName = "l_mem",
                    Template = "memory32"
                },
                new SOCRecord() {
                    Segment = 0x01,
                    DataType = typeof(ushort),
                    Depth = 16,
                    SoftwareName = "data",
                    HardwareName = "data",
                    Template = "memory16"
                },
            };

            var mainCode = @"
    uint32_t counter = 0;
       
    while (counter < data_size) {
		data[counter] = counter;
        counter = data[counter] + 1;
    } 
";
            await RunWithData(externalData, mainCode);
        }

        [TestMethod]
        public async Task RISCV_Memory_Byte()
        {
            var externalData = new List<SOCRecord>()
            {
                new SOCRecord() {
                    Segment = 0x00,
                    DataType = typeof(uint),
                    Depth = 512,
                    SoftwareName = "l_mem",
                    HardwareName = "l_mem",
                    Template = "memory32"
                },
                new SOCRecord() {
                    Segment = 0x01,
                    DataType = typeof(byte),
                    Depth = 16,
                    SoftwareName = "data",
                    HardwareName = "data",
                    Template = "memory8"
                },
            };

            var mainCode = @"
    uint32_t counter = 0;
       
    while (counter < data_size) {
		data[counter] = counter;
        counter = data[counter] + 1;
    } 
";
            await RunWithData(externalData, mainCode);
        }

        [TestMethod]
        public async Task ClientTest_Docker_Blinker()
        {
            //if (!Debugger.IsAttached)
            //    Assert.Inconclusive("Run local service and debug this test");

            var externalData = new List<SOCRecord>()
            {
                new SOCRecord() {
                    Segment = 0x00,
                    DataType = typeof(uint),
                    Depth = 512,
                    SoftwareName = "l_mem",
                    HardwareName = "l_mem",
                    Template = "memory32"
                },
                new SOCRecord() {
                    Segment = 0x01,
                    DataType = typeof(uint),
                    SoftwareName = "LED1",
                    HardwareName = "led1",
                    Template = "register"
                },
            };

            var mainCode = @"
    // blink the user LED
    uint32_t led_timer = 0;
       
    while (1) {
        LED1 = led_timer >> 16;
        led_timer = led_timer + 1;
    } 
";

            await RunWithData(externalData, mainCode);
        }

        async Task RunWithData(
            List<SOCRecord> externalData,
            string mainCode)
        {
            var textReplacer = new TextReplacer();

            var templateRoot = TemplatesPath(Path.GetDirectoryName(Directory.GetCurrentDirectory()));
            var sourceRoot = @"C:\code\Quokka.RISCV.Docker.Server\Quokka.RISCV.Integration.Tests\Client\Blinker\Source";

            var context = new RISCVIntegrationContext()
                .WithExtensionClasses(
                    new ExtensionClasses()
                        .Text("")
                        .Text("lds")
                        .Text("s")
                        .Text("c")
                        .Text("cpp")
                        .Text("h")
                        .Binary("bin")
                        .Binary("elf")
                        .Text("map")
                )
                .WithRootFolder(sourceRoot)
                .WithAllRegisteredFiles()
                .WithOperations(
                    new BashInvocation("make firmware.bin")
                )
                .TakeModifiedFiles()
                ;

            var firmwareTemplatePath = File.ReadAllText(Path.Combine(templateRoot, "firmware.template.cpp"));
            var firmwareMap = new Dictionary<string, string>()
            {
                { "MAIN_CODE", mainCode }
            };
            firmwareTemplatePath = textReplacer.ReplaceToken(firmwareTemplatePath, firmwareMap);

            var dmaGenerator = new SOCGenerator();
            context.SourceSnapshot.Files.Add(dmaGenerator.SOCImport(externalData));

            var generator = new IntegrationGenerator();
            context.SourceSnapshot.Files.Add(generator.Firmware(firmwareTemplatePath));

            new FSManager(sourceRoot).SaveSnapshot(context.SourceSnapshot);

            var result = await RISCVIntegrationClient.Run(context);
            Assert.IsNotNull(result);

            var hardwareTemplatePath = Path.Combine(templateRoot, "hardware.template.v");
            var hardwareTemplate = File.ReadAllText(hardwareTemplatePath);

            // memory init file
            var binFile = (FSBinaryFile)result.ResultSnapshot.Files.Find(f => f.Name == "firmware.bin");
            Assert.IsNotNull(binFile);

            var replacers = new Dictionary<string, string>();

            var words = TestTools.ReadWords(binFile.Content).ToList();
            var memInit = generator.MemInit(words, "l_mem", 512, 4);

            replacers["MEM_INIT"] = memInit;

            // data declarations
            replacers["DATA_DECL"] = generator.DataDeclaration(externalData);

            // data control signals
            var templates = new IntegrationTemplates();
            foreach (var templatePath in Directory.EnumerateFiles(templateRoot, "*.*", SearchOption.AllDirectories))
            {
                var name = Path.GetFileName(templatePath).Split('.')[0];
                templates.Templates[name] = File.ReadAllText(templatePath);
            }

            replacers["DATA_CTRL"] = generator.DataControl(externalData, templates);
            replacers["MEM_READY"] = generator.MemReady(externalData);
            replacers["MEM_RDATA"] = generator.MemRData(externalData);

            hardwareTemplate = textReplacer.ReplaceToken(hardwareTemplate, replacers);

            File.WriteAllText(@"C:\code\picorv32\quartus\RVTest.v", hardwareTemplate);
        }
    }
}
